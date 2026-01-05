using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using System.Security.Claims;

namespace EsportsTournament.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BracketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BracketsController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost("generate/{tournamentId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> GenerateBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return NotFound("Nie znaleziono turnieju.");

            if (await _context.Matches.AnyAsync(m => m.TournamentId == tournamentId))
                return BadRequest("Drabinka już istnieje.");

            List<int> participantIds = new List<int>();
            string pType = "";

            if (tournament.RegistrationType.ToLower() == "team")
            {
                pType = "team";
                participantIds = await _context.TournamentRegistrationsTeam
                    .Where(r => r.TournamentId == tournamentId && r.Status == "Confirmed")
                    .Select(r => r.TeamId)
                    .ToListAsync();
            }
            else
            {
                pType = "user";
                participantIds = await _context.TournamentRegistrations
                    .Where(r => r.TournamentId == tournamentId && r.Status == "Approved")
                    .Select(r => r.UserId)
                    .ToListAsync();
            }

            int count = participantIds.Count;

            if (count < 2 || (count & (count - 1)) != 0)
                return BadRequest($"Liczba uczestników ({count}) musi być potęgą dwójki (2, 4, 8, 16, 32...). Usuń nadmiarowe drużyny lub znajdź brakujące.");

            var rng = new Random();
            var shuffled = participantIds.OrderBy(x => rng.Next()).ToList();

            var matchesToAdd = new List<Match>();
            int totalRounds = (int)Math.Log2(count);

            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound = count / (int)Math.Pow(2, round);

                for (int matchNum = 1; matchNum <= matchesInRound; matchNum++)
                {
                    var match = new Match
                    {
                        TournamentId = tournamentId,
                        RoundNumber = round,
                        MatchNumber = matchNum,
                        MatchStatus = "scheduled",
                        CreatedAt = DateTime.UtcNow,
                        Participant1Type = pType,
                        Participant2Type = pType
                    };

                    if (round == 1)
                    {
                        int idx = (matchNum - 1) * 2;
                        match.Participant1Id = shuffled[idx];
                        match.Participant2Id = shuffled[idx + 1];
                    }
                    else
                    {
                        match.Participant1Id = null;
                        match.Participant2Id = null;
                    }

                    matchesToAdd.Add(match);
                }
            }

            _context.Matches.AddRange(matchesToAdd);
            tournament.Status = "in_progress";

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Drabinka wygenerowana!", MatchesCount = matchesToAdd.Count });
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetBracket(int tournamentId)
        {
            var matches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.RoundNumber)
                .ThenBy(m => m.MatchNumber)
                .ToListAsync();

            var teamIds = matches.Where(m => m.Participant1Type == "team")
                                 .SelectMany(m => new[] { m.Participant1Id, m.Participant2Id })
                                 .OfType<int>().Distinct().ToList();

            var userIds = matches.Where(m => m.Participant1Type == "user")
                                 .SelectMany(m => new[] { m.Participant1Id, m.Participant2Id })
                                 .OfType<int>().Distinct().ToList();

            var teams = await _context.Teams
                .Where(t => teamIds.Contains(t.TeamId))
                .ToDictionaryAsync(t => t.TeamId, t => t.TeamName);

            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Username);

            var matchDtos = matches.Select(m => new MatchDto
            {
                MatchId = m.MatchId,
                MatchNumber = m.MatchNumber,
                RoundNumber = m.RoundNumber,
                MatchStatus = m.MatchStatus,
                Participant1Id = m.Participant1Id,
                Participant2Id = m.Participant2Id,
                WinnerId = m.WinnerId,

                Participant1Name = m.Participant1Id.HasValue
                    ? (m.Participant1Type == "team"
                        ? (teams.ContainsKey(m.Participant1Id.Value) ? teams[m.Participant1Id.Value] : "Team " + m.Participant1Id)
                        : (users.ContainsKey(m.Participant1Id.Value) ? users[m.Participant1Id.Value] : "User " + m.Participant1Id))
                    : "TBA", 

                Participant2Name = m.Participant2Id.HasValue
                    ? (m.Participant2Type == "team"
                        ? (teams.ContainsKey(m.Participant2Id.Value) ? teams[m.Participant2Id.Value] : "Team " + m.Participant2Id)
                        : (users.ContainsKey(m.Participant2Id.Value) ? users[m.Participant2Id.Value] : "User " + m.Participant2Id))
                    : "TBA"
            });

            return Ok(matchDtos);
        }

        [HttpPost("report-result")]
        [Authorize]
        public async Task<IActionResult> ReportResult([FromBody] MatchResultDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var match = await _context.Matches.FindAsync(dto.MatchId);
            if (match == null) return NotFound("Mecz nie istnieje.");
            if (match.MatchStatus == "finished") return BadRequest("Ten mecz jest już zakończony.");

            bool isParticipant = false;

            if (match.Participant1Type == "team")
            {
                isParticipant = await _context.Teams.AnyAsync(t => (t.TeamId == match.Participant1Id || t.TeamId == match.Participant2Id) && t.CaptainId == userId);
            }
            else
            {
                isParticipant = (match.Participant1Id == userId || match.Participant2Id == userId);
            }

            if (!isParticipant)
                return StatusCode(403, "Nie jesteś uczestnikiem lub kapitanem w tym meczu.");

            var existingReport = await _context.MatchResults
                .FirstOrDefaultAsync(r => r.MatchId == dto.MatchId && r.ResultStatus == "pending");

            if (existingReport != null)
                return BadRequest("Wynik został już zgłoszony i czeka na akceptację.");

            var result = new MatchResult
            {
                MatchId = dto.MatchId,
                Participant1Score = dto.ScoreA,
                Participant2Score = dto.ScoreB,
                ReportedBy = userId,
                ResultStatus = "pending",
                ScreenshotUrl = dto.ScreenshotUrl,
                ReportedAt = DateTime.UtcNow
            };

            _context.MatchResults.Add(result);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Wynik zgłoszony. Czekaj na potwierdzenie przeciwnika." });
        }

        [HttpPost("accept-result/{resultId}")]
        [Authorize]
        public async Task<IActionResult> AcceptResult(int resultId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var result = await _context.MatchResults
                .Include(r => r.Match)
                .FirstOrDefaultAsync(r => r.ResultId == resultId);

            if (result == null) return NotFound("Nie znaleziono zgłoszenia.");
            if (result.ResultStatus != "pending") return BadRequest("Ten wynik nie oczekuje na potwierdzenie.");

            var match = result.Match;
            if (userId == result.ReportedBy)
                return BadRequest("Nie możesz zaakceptować własnego zgłoszenia!");

            bool isParticipant = false;

            if (match.Participant1Type == "team")
            {
                isParticipant = await _context.Teams.AnyAsync(t => (t.TeamId == match.Participant1Id || t.TeamId == match.Participant2Id) && t.CaptainId == userId);
            }
            else
            {
                isParticipant = (match.Participant1Id == userId || match.Participant2Id == userId);
            }

            if (!isParticipant) return StatusCode(403, "Nie bierzesz udziału w tym meczu.");

            result.ResultStatus = "confirmed";
            result.ConfirmedBy = userId;
            result.ConfirmedAt = DateTime.UtcNow;

            await FinalizeMatchAndAdvance(match, result.Participant1Score, result.Participant2Score);

            return Ok(new { Message = "Wynik zaakceptowany! Zwycięzca przechodzi dalej." });
        }


        [HttpPost("dispute-result/{resultId}")]
        [Authorize]
        public async Task<IActionResult> DisputeResult(int resultId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var result = await _context.MatchResults.FindAsync(resultId);

            if (result == null) return NotFound();
            if (result.ReportedBy == userId) return BadRequest("Nie możesz oprotestować własnego wyniku.");

            result.ResultStatus = "disputed";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Zgłoszono sprzeciw. Admin sprawdzi ten mecz." });
        }

        [HttpPost("admin-resolve/{matchId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> AdminResolveMatch(int matchId, [FromBody] MatchResultDto finalResult)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound();

            var oldResults = _context.MatchResults.Where(r => r.MatchId == matchId);
            _context.MatchResults.RemoveRange(oldResults);

            var adminResult = new MatchResult
            {
                MatchId = matchId,
                Participant1Score = finalResult.ScoreA,
                Participant2Score = finalResult.ScoreB,
                ResultStatus = "confirmed_by_admin",
                ReportedAt = DateTime.UtcNow,
                Notes = "Rozstrzygnięcie administracyjne."
            };
            _context.MatchResults.Add(adminResult);

            await FinalizeMatchAndAdvance(match, finalResult.ScoreA, finalResult.ScoreB);

            return Ok(new { Message = "Spór rozwiązany. Wynik zaktualizowany." });
        }


        private async Task FinalizeMatchAndAdvance(Match match, int scoreA, int scoreB)
        {
            match.MatchStatus = "finished";

            int winnerId;
            string winnerType = match.Participant1Type;

            if (scoreA > scoreB)
                winnerId = match.Participant1Id.Value;
            else
                winnerId = match.Participant2Id.Value;

            match.WinnerId = winnerId;
            match.WinnerType = winnerType;

            int nextRound = match.RoundNumber + 1;
            int nextMatchNum = (match.MatchNumber + 1) / 2;

            var nextMatch = await _context.Matches
                .FirstOrDefaultAsync(m => m.TournamentId == match.TournamentId &&
                                          m.RoundNumber == nextRound &&
                                          m.MatchNumber == nextMatchNum);

            if (nextMatch != null)
            {
                bool isOddMatch = (match.MatchNumber % 2 != 0);

                if (isOddMatch)
                {
                    nextMatch.Participant1Id = winnerId;
                    nextMatch.Participant1Type = winnerType;
                }
                else
                {
                    nextMatch.Participant2Id = winnerId;
                    nextMatch.Participant2Type = winnerType;
                }
            }
            else
            {
                var tournament = await _context.Tournaments.FindAsync(match.TournamentId);
                if (tournament != null) tournament.Status = "finished";
            }

            await _context.SaveChangesAsync();
        }
    }

    public class MatchResultDto
    {
        public int MatchId { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public string? ScreenshotUrl { get; set; }
    }

    public class MatchDto
    {
        public int MatchId { get; set; }
        public int MatchNumber { get; set; }
        public int RoundNumber { get; set; }
        public string MatchStatus { get; set; }

        public int? Participant1Id { get; set; }
        public string? Participant1Name { get; set; } // Tu trafi nazwa

        public int? Participant2Id { get; set; }
        public string? Participant2Name { get; set; } // Tu trafi nazwa

        public int? WinnerId { get; set; }
    }
}