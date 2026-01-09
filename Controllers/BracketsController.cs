using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using EsportsTournament.API.Models.DTOs;
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

        // ... [Metoda GenerateBracket pozostaje bez zmian] ...
        [HttpPost("generate/{tournamentId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> GenerateBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return NotFound("Nie znaleziono turnieju.");

            if (await _context.Matches.AnyAsync(m => m.TournamentId == tournamentId))
                return BadRequest("Drabinka już istnieje. Usuń ją najpierw, aby wygenerować nową.");

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
                participantIds = await _context.TournamentRegistrationIndividual
                    .Where(r => r.TournamentId == tournamentId && r.Status.ToLower() == "confirmed")
                    .Select(r => r.UserId)
                    .ToListAsync();
            }

            int count = participantIds.Count;
            if (count < 2 || (count & (count - 1)) != 0)
            {
                int nextPow2 = (int)Math.Pow(2, Math.Ceiling(Math.Log2(count)));
                return BadRequest($"Liczba uczestników ({count}) musi być potęgą dwójki (2, 4, 8, 16...).");
            }

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

        // --- ZMODYFIKOWANA METODA GET BRACKET ---
        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetBracket(int tournamentId)
        {
            var matches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .Include(m => m.MatchResults)
                .OrderBy(m => m.RoundNumber)
                .ThenBy(m => m.MatchNumber)
                .ToListAsync();

            if (!matches.Any()) return NotFound("Brak drabinki.");

            // Pobieramy ID wszystkich uczestników
            var teamIds = matches.Where(m => m.Participant1Type == "team")
                                 .SelectMany(m => new[] { m.Participant1Id, m.Participant2Id })
                                 .OfType<int>().Distinct().ToList();

            var userIds = matches.Where(m => m.Participant1Type == "user")
                                 .SelectMany(m => new[] { m.Participant1Id, m.Participant2Id })
                                 .OfType<int>().Distinct().ToList();

            // Pobieramy info o drużynach (w tym Kapitana!)
            var teamsData = await _context.Teams
                .Where(t => teamIds.Contains(t.TeamId))
                .ToDictionaryAsync(t => t.TeamId, t => new { t.TeamName, t.CaptainId });

            // Pobieramy info o userach
            var usersData = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Username);

            var matchDtos = matches.Select(m =>
            {
                var pendingResult = m.MatchResults.FirstOrDefault(r => r.ResultStatus == "pending");

                // Logika wyciągania Kapitana
                int? GetCaptainId(int? pId, string type)
                {
                    if (!pId.HasValue) return null;
                    if (type == "team" && teamsData.ContainsKey(pId.Value)) return teamsData[pId.Value].CaptainId;
                    if (type == "user") return pId.Value; // W trybie solo gracz jest swoim kapitanem
                    return null;
                }

                // Logika nazwy
                string GetName(int? pId, string type)
                {
                    if (!pId.HasValue) return "TBA";
                    if (type == "team" && teamsData.ContainsKey(pId.Value)) return teamsData[pId.Value].TeamName;
                    if (type == "user" && usersData.ContainsKey(pId.Value)) return usersData[pId.Value];
                    return "Unknown";
                }

                return new MatchDto
                {
                    MatchId = m.MatchId,
                    MatchNumber = m.MatchNumber,
                    RoundNumber = m.RoundNumber,
                    MatchStatus = pendingResult != null ? "pending" : m.MatchStatus,

                    Participant1Id = m.Participant1Id,
                    Participant1Name = GetName(m.Participant1Id, m.Participant1Type),
                    Participant1CaptainId = GetCaptainId(m.Participant1Id, m.Participant1Type), // <--- Przypisanie

                    Participant2Id = m.Participant2Id,
                    Participant2Name = GetName(m.Participant2Id, m.Participant2Type),
                    Participant2CaptainId = GetCaptainId(m.Participant2Id, m.Participant2Type), // <--- Przypisanie

                    WinnerId = m.WinnerId,
                    Score1 = (m.MatchStatus == "finished" && m.WinnerId == m.Participant1Id) ? 1 : 0,
                    Score2 = (m.MatchStatus == "finished" && m.WinnerId == m.Participant2Id) ? 1 : 0,

                    PendingResult = pendingResult != null ? new PendingResultDto
                    {
                        ResultId = pendingResult.ResultId,
                        ScoreA = pendingResult.Participant1Score,
                        ScoreB = pendingResult.Participant2Score,
                        ReportedBy = pendingResult.ReportedBy
                    } : null
                };
            });

            return Ok(matchDtos);
        }

        // ... [Reszta metod: ReportResult, AcceptResult, DisputeResult, AdminResolve, DeleteBracket - bez zmian] ...
        [HttpPost("report-result")]
        [Authorize]
        public async Task<IActionResult> ReportResult([FromBody] MatchResultDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var match = await _context.Matches.FindAsync(dto.MatchId);
            if (match == null) return NotFound("Mecz nie istnieje.");
            if (match.MatchStatus == "finished") return BadRequest("Mecz zakończony.");

            bool isParticipant = false;
            if (match.Participant1Type == "team")
                isParticipant = await _context.Teams.AnyAsync(t => (t.TeamId == match.Participant1Id || t.TeamId == match.Participant2Id) && t.CaptainId == userId);
            else
                isParticipant = (match.Participant1Id == userId || match.Participant2Id == userId);

            if (!isParticipant) return StatusCode(403, "Brak uprawnień.");

            var existingReport = await _context.MatchResults
                .FirstOrDefaultAsync(r => r.MatchId == dto.MatchId && r.ResultStatus == "pending");

            if (existingReport != null) return BadRequest("Wynik już zgłoszony.");

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
            match.MatchStatus = "pending";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Wynik zgłoszony." });
        }

        [HttpPost("accept-result/{resultId}")]
        [Authorize]
        public async Task<IActionResult> AcceptResult(int resultId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var result = await _context.MatchResults.Include(r => r.Match).FirstOrDefaultAsync(r => r.ResultId == resultId);
            if (result == null) return NotFound();

            var match = result.Match;
            if (match == null) return NotFound("Nie znaleziono meczu powiązanego z wynikiem.");

            if (result.ReportedBy == userId) return BadRequest("Nie możesz zaakceptować własnego zgłoszenia!");

            bool isParticipant = false;
            if (match.Participant1Type == "team")
                isParticipant = await _context.Teams.AnyAsync(t => (t.TeamId == match.Participant1Id || t.TeamId == match.Participant2Id) && t.CaptainId == userId);
            else
                isParticipant = (match.Participant1Id == userId || match.Participant2Id == userId);

            if (!isParticipant) return StatusCode(403, "Brak uprawnień.");

            result.ResultStatus = "confirmed";
            result.ConfirmedBy = userId;
            result.ConfirmedAt = DateTime.UtcNow;

            await FinalizeMatchAndAdvance(match, result.Participant1Score, result.Participant2Score);
            return Ok(new { Message = "Zatwierdzono!" });
        }

        [HttpPost("dispute-result/{resultId}")]
        [Authorize]
        public async Task<IActionResult> DisputeResult(int resultId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var result = await _context.MatchResults.Include(r => r.Match).FirstOrDefaultAsync(r => r.ResultId == resultId);
            if (result == null) return NotFound();

            if (result.Match == null) return NotFound("Mecz nie istnieje.");

            if (result.ReportedBy == userId) return BadRequest("Nie możesz oprotestować własnego zgłoszenia.");

            result.ResultStatus = "disputed";
            result.Match.MatchStatus = "disputed";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Spór zgłoszony." });
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
                ReportedAt = DateTime.UtcNow
            };
            _context.MatchResults.Add(adminResult);

            await FinalizeMatchAndAdvance(match, finalResult.ScoreA, finalResult.ScoreB);
            return Ok(new { Message = "Spór rozwiązany." });
        }

        [HttpDelete("delete/{tournamentId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> DeleteBracket(int tournamentId)
        {
            var matches = await _context.Matches.Where(m => m.TournamentId == tournamentId).ToListAsync();
            if (!matches.Any()) return NotFound("Brak drabinki.");

            var matchIds = matches.Select(m => m.MatchId).ToList();
            var results = await _context.MatchResults.Where(r => matchIds.Contains(r.MatchId)).ToListAsync();

            _context.MatchResults.RemoveRange(results);
            _context.Matches.RemoveRange(matches);

            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament != null) tournament.Status = "registration";

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Drabinka usunięta." });
        }

        private async Task FinalizeMatchAndAdvance(Match match, int scoreA, int scoreB)
        {
            match.MatchStatus = "finished";
            int winnerId = (scoreA > scoreB) ? match.Participant1Id!.Value : match.Participant2Id!.Value;

            match.WinnerId = winnerId;
            match.WinnerType = match.Participant1Type;

            int nextRound = match.RoundNumber + 1;
            int nextMatchNum = (int)Math.Ceiling((double)match.MatchNumber / 2);

            var nextMatch = await _context.Matches
                .FirstOrDefaultAsync(m => m.TournamentId == match.TournamentId &&
                                          m.RoundNumber == nextRound &&
                                          m.MatchNumber == nextMatchNum);

            if (nextMatch != null)
            {
                bool isOddMatch = (match.MatchNumber % 2 != 0);
                if (isOddMatch) { nextMatch.Participant1Id = winnerId; nextMatch.Participant1Type = match.WinnerType; }
                else { nextMatch.Participant2Id = winnerId; nextMatch.Participant2Type = match.WinnerType; }
            }
            else
            {
                var tournament = await _context.Tournaments.FindAsync(match.TournamentId);
                if (tournament != null) tournament.Status = "finished";
            }
            await _context.SaveChangesAsync();
        }
    }
}