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

        [HttpPost("generate/{tournamentId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> GenerateBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return NotFound("Nie znaleziono turnieju.");

            if (await _context.Matches.AnyAsync(m => m.TournamentId == tournamentId))
                return BadRequest("Drabinka już istnieje. Usuń ją najpierw.");

            List<int> participantIds = new List<int>();
            string pType = "";

            if (tournament.RegistrationType?.ToLower() == "team")
            {
                pType = "team";
                participantIds = await _context.TournamentRegistrationsTeam
                    .Where(r => r.TournamentId == tournamentId && r.Status == "Confirmed")
                    .Select(r => r.TeamId)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                pType = "user";
                participantIds = await _context.TournamentRegistrationIndividual
                    .Where(r => r.TournamentId == tournamentId && r.Status.ToLower() == "confirmed")
                    .Select(r => r.UserId)
                    .Distinct()
                    .ToListAsync();
            }

            int count = participantIds.Count;
            if (count < 2) return BadRequest($"Za mało uczestników: {count}. Wymagane minimum 2.");

            int nextPow2 = (int)Math.Pow(2, Math.Ceiling(Math.Log2(count)));
            int byes = nextPow2 - count;

            var rng = new Random();
            var shuffled = participantIds.OrderBy(x => rng.Next()).ToList();

            var matchesToAdd = new List<Match>();
            int totalRounds = (int)Math.Log2(nextPow2);

            for (int round = 1; round <= totalRounds; round++)
            {
                int matchesInRound = nextPow2 / (int)Math.Pow(2, round);
                for (int matchNum = 1; matchNum <= matchesInRound; matchNum++)
                {
                    matchesToAdd.Add(new Match
                    {
                        TournamentId = tournamentId,
                        RoundNumber = round,
                        MatchNumber = matchNum,
                        MatchStatus = "scheduled",
                        CreatedAt = DateTime.UtcNow,
                        Participant1Type = pType,
                        Participant2Type = pType
                    });
                }
            }

            var round1Matches = matchesToAdd.Where(m => m.RoundNumber == 1).OrderBy(m => m.MatchNumber).ToList();
            int pIndex = 0;

            for (int i = 0; i < round1Matches.Count; i++)
            {
                var match = round1Matches[i];

                if (pIndex < shuffled.Count)
                {
                    match.Participant1Id = shuffled[pIndex];
                    pIndex++;
                }

                if (pIndex < shuffled.Count)
                {
                    match.Participant2Id = shuffled[pIndex];
                    pIndex++;
                }
                else
                {
                    match.Participant2Id = null;
                    if (match.Participant1Id.HasValue)
                    {
                        match.MatchStatus = "finished";
                        match.WinnerId = match.Participant1Id;
                        match.WinnerType = pType;
                    }
                }
            }

            _context.Matches.AddRange(matchesToAdd);
            tournament.Status = "in_progress";
            await _context.SaveChangesAsync();

            var byeMatches = matchesToAdd.Where(m => m.RoundNumber == 1 && m.MatchStatus == "finished").ToList();
            foreach (var bm in byeMatches)
            {
                await FinalizeMatchAndAdvance(bm, 1, 0);
            }

            return Ok(new { Message = $"Drabinka wygenerowana! Uczestników: {count}. Wolne losy: {byes}." });
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetBracket(int tournamentId)
        {
            var matches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .Include(m => m.MatchResults)
                .OrderBy(m => m.RoundNumber)
                .ThenBy(m => m.MatchNumber)
                .ToListAsync();

            if (!matches.Any()) return Ok(new List<MatchDto>());

            var teamIds = matches.Where(m => m.Participant1Type == "team")
                                 .SelectMany(m => new[] { m.Participant1Id, m.Participant2Id })
                                 .OfType<int>().Distinct().ToList();

            var userIds = matches.Where(m => m.Participant1Type == "user")
                                 .SelectMany(m => new[] { m.Participant1Id, m.Participant2Id })
                                 .OfType<int>().Distinct().ToList();

            var teamsData = await _context.Teams
                .Where(t => teamIds.Contains(t.TeamId))
                .ToDictionaryAsync(t => t.TeamId, t => new { t.TeamName, t.CaptainId });

            var usersData = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Username);

            var matchDtos = matches.Select(m =>
            {
                var pendingResult = m.MatchResults.FirstOrDefault(r => r.ResultStatus == "pending");
                var confirmedResult = m.MatchResults.FirstOrDefault(r => r.ResultStatus == "confirmed" || r.ResultStatus == "confirmed_by_admin");

                int? GetCaptainId(int? pId, string? type)
                {
                    if (!pId.HasValue || string.IsNullOrEmpty(type)) return null;
                    if (type == "team" && teamsData.ContainsKey(pId.Value)) return teamsData[pId.Value].CaptainId;
                    if (type == "user") return pId.Value;
                    return null;
                }

                string GetName(int? pId, string? type)
                {
                    if (!pId.HasValue || string.IsNullOrEmpty(type)) return "WOLNY LOS";
                    if (type == "team" && teamsData.ContainsKey(pId.Value)) return teamsData[pId.Value].TeamName;
                    if (type == "user" && usersData.ContainsKey(pId.Value)) return usersData[pId.Value];
                    return "Unknown";
                }

                int? score1 = null;
                int? score2 = null;

                if (m.MatchStatus == "finished")
                {
                    if (confirmedResult != null)
                    {
                        score1 = confirmedResult.Participant1Score;
                        score2 = confirmedResult.Participant2Score;
                    }
                    else if (m.WinnerId.HasValue)
                    {
                        score1 = (m.WinnerId == m.Participant1Id) ? 1 : 0;
                        score2 = (m.WinnerId == m.Participant2Id) ? 1 : 0;
                    }
                }

                return new MatchDto
                {
                    MatchId = m.MatchId,
                    MatchNumber = m.MatchNumber,
                    RoundNumber = m.RoundNumber,
                    MatchStatus = pendingResult != null ? "pending" : m.MatchStatus,

                    Participant1Id = m.Participant1Id,
                    Participant1Name = GetName(m.Participant1Id, m.Participant1Type),
                    Participant1CaptainId = GetCaptainId(m.Participant1Id, m.Participant1Type),

                    Participant2Id = m.Participant2Id,
                    Participant2Name = GetName(m.Participant2Id, m.Participant2Type),
                    Participant2CaptainId = GetCaptainId(m.Participant2Id, m.Participant2Type),

                    WinnerId = m.WinnerId,
                    Score1 = score1,
                    Score2 = score2,

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

            bool isP1 = false;
            bool isP2 = false;

            if (match.Participant1Type == "team")
            {
                isP1 = await _context.Teams.AnyAsync(t => t.TeamId == match.Participant1Id && t.CaptainId == userId);
                isP2 = await _context.Teams.AnyAsync(t => t.TeamId == match.Participant2Id && t.CaptainId == userId);
            }
            else
            {
                isP1 = (match.Participant1Id == userId);
                isP2 = (match.Participant2Id == userId);
            }

            if (!isP1 && !isP2) return StatusCode(403, "Brak uprawnień.");

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

            int? opponentId = isP1 ? match.Participant2Id : match.Participant1Id;
            int? notificationReceiverId = null;

            if (opponentId.HasValue)
            {
                if (match.Participant1Type == "team")
                {
                    var opTeam = await _context.Teams.FindAsync(opponentId.Value);
                    notificationReceiverId = opTeam?.CaptainId;
                }
                else
                {
                    notificationReceiverId = opponentId.Value;
                }
            }

            if (notificationReceiverId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = notificationReceiverId.Value,
                    Title = "Wynik zgłoszony",
                    Message = "Przeciwnik zgłosił wynik meczu. Wejdź w drabinkę i go zatwierdź lub zgłoś sprzeciw.",
                    NotificationType = "MatchReport",
                    RelatedId = match.MatchId,
                    RelatedType = "Match",
                    CreatedAt = DateTime.UtcNow
                });
            }

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
            if (match == null) return NotFound("Nie znaleziono meczu.");

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
        public async Task<IActionResult> DisputeResult(int resultId, [FromBody] DisputeResultDto dto)
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
            result.Notes = $"SPÓR zgłoszony przez ID {userId}. Powód: {dto.Reason}. Dowód: {dto.ProofUrl ?? "brak"}";

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Spór zgłoszony." });
        }

        [HttpPost("admin-resolve/{matchId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> AdminResolveMatch(int matchId, [FromBody] MatchResultDto finalResult)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int adminId = int.Parse(userIdString);

            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound();

            if (match.Participant1Id == null || match.Participant2Id == null)
            {
                return BadRequest("Nie można rozstrzygnąć meczu bez uczestników.");
            }

            var oldResults = _context.MatchResults.Where(r => r.MatchId == matchId);
            _context.MatchResults.RemoveRange(oldResults);

            var adminResult = new MatchResult
            {
                MatchId = matchId,
                Participant1Score = finalResult.ScoreA,
                Participant2Score = finalResult.ScoreB,
                ResultStatus = "confirmed_by_admin",
                ReportedAt = DateTime.UtcNow,
                ReportedBy = adminId,
                ConfirmedBy = adminId,
                ConfirmedAt = DateTime.UtcNow,
                Notes = "Rozstrzygnięcie administracyjne"
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
            int loserId = (scoreA > scoreB) ? match.Participant2Id!.Value : match.Participant1Id!.Value;

            match.WinnerId = winnerId;
            match.WinnerType = match.Participant1Type;

            await SendMatchResultNotifications(match, winnerId, loserId);

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

        private async Task SendMatchResultNotifications(Match match, int winnerId, int loserId)
        {
            string winnerName = "Your Team";
            string loserName = "Opponent Team";

            List<int> winnerMemberIds = new List<int>();
            List<int> loserMemberIds = new List<int>();

            if (match.Participant1Type == "team")
            {
                var wTeam = await _context.Teams.FindAsync(winnerId);
                var lTeam = await _context.Teams.FindAsync(loserId);
                winnerName = wTeam?.TeamName ?? "Unknown";
                loserName = lTeam?.TeamName ?? "Unknown";

                winnerMemberIds = await _context.TeamMembers
                    .Where(tm => tm.TeamId == winnerId && tm.Status == "Member")
                    .Select(tm => tm.UserId)
                    .ToListAsync();

                loserMemberIds = await _context.TeamMembers
                    .Where(tm => tm.TeamId == loserId && tm.Status == "Member")
                    .Select(tm => tm.UserId)
                    .ToListAsync();
            }
            else
            {
                var wUser = await _context.Users.FindAsync(winnerId);
                var lUser = await _context.Users.FindAsync(loserId);
                winnerName = wUser?.Username ?? "Unknown";
                loserName = lUser?.Username ?? "Unknown";

                winnerMemberIds.Add(winnerId);
                loserMemberIds.Add(loserId);
            }

            foreach (var userId in winnerMemberIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Zwycięstwo!",
                    Message = $"Gratulacje! Wygrałeś mecz przeciwko {loserName}.",
                    NotificationType = "MatchResult",
                    RelatedId = match.MatchId,
                    RelatedType = "Match"
                });
            }

            foreach (var userId in loserMemberIds)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Przegrana",
                    Message = $"Niestety, przegrałeś mecz przeciwko {winnerName}.",
                    NotificationType = "MatchResult",
                    RelatedId = match.MatchId,
                    RelatedType = "Match"
                });
            }
        }
    }
}