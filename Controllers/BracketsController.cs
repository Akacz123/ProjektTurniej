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

        // --- GENEROWANIE DRABINKI ---
        [HttpPost("generate/{tournamentId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> GenerateBracket(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return NotFound("Nie znaleziono turnieju.");

            // Sprawdź czy drabinka już istnieje
            if (await _context.Matches.AnyAsync(m => m.TournamentId == tournamentId))
                return BadRequest("Drabinka już istnieje. Usuń ją najpierw, aby wygenerować nową.");

            List<int> participantIds = new List<int>();
            string pType = "";

            // 1. POBIERANIE UCZESTNIKÓW (POPRAWIONE)
            if (tournament.RegistrationType.ToLower() == "team")
            {
                pType = "team";
                // Pobieramy ID zespołów, które są "Confirmed" I istnieją w tabeli Teams
                participantIds = await _context.TournamentRegistrationsTeam
                    .Where(r => r.TournamentId == tournamentId && r.Status == "Confirmed")
                    .Join(_context.Teams, // Join żeby upewnić się, że team istnieje
                          reg => reg.TeamId,
                          team => team.TeamId,
                          (reg, team) => team.TeamId)
                    .ToListAsync();
            }
            else
            {
                pType = "user";
                // POPRAWKA: Używamy TournamentRegistrationIndividual zamiast generic
                // POPRAWKA: Status to "confirmed" (mała litera), a nie "Approved"
                participantIds = await _context.TournamentRegistrationIndividual
                    .Where(r => r.TournamentId == tournamentId && r.Status.ToLower() == "confirmed")
                    .Join(_context.Users, // Join żeby upewnić się, że user istnieje
                          reg => reg.UserId,
                          user => user.UserId,
                          (reg, user) => user.UserId)
                    .ToListAsync();
            }

            int count = participantIds.Count;

            // 2. WALIDACJA POTĘGI DWÓJKI
            // 2, 4, 8, 16, 32...
            if (count < 2 || (count & (count - 1)) != 0)
            {
                // Znajdź najbliższą potęgę dwójki w górę dla info
                int nextPow2 = (int)Math.Pow(2, Math.Ceiling(Math.Log2(count)));
                return BadRequest($"Liczba uczestników wynosi {count}. Wymagana potęga dwójki (2, 4, 8, 16...). Brakuje {nextPow2 - count} uczestników lub masz ich za dużo.");
            }

            // 3. TASOWANIE (SEEDING LOSOWY)
            var rng = new Random();
            var shuffled = participantIds.OrderBy(x => rng.Next()).ToList();

            var matchesToAdd = new List<Match>();
            int totalRounds = (int)Math.Log2(count);

            // 4. GENEROWANIE MECZY
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
                        MatchStatus = "scheduled", // Domyślny status
                        CreatedAt = DateTime.UtcNow,
                        Participant1Type = pType,
                        Participant2Type = pType
                    };

                    // Tylko w 1. rundzie przypisujemy konkretnych uczestników
                    if (round == 1)
                    {
                        int idx = (matchNum - 1) * 2;
                        match.Participant1Id = shuffled[idx];
                        match.Participant2Id = shuffled[idx + 1];
                    }
                    else
                    {
                        // W kolejnych rundach czekamy na zwycięzców (TBA)
                        match.Participant1Id = null;
                        match.Participant2Id = null;
                    }

                    matchesToAdd.Add(match);
                }
            }

            _context.Matches.AddRange(matchesToAdd);
            tournament.Status = "in_progress"; // Zmieniamy status turnieju

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Drabinka wygenerowana! Liczba drużyn: {count}, Liczba meczów: {matchesToAdd.Count}" });
        }

        // --- POBIERANIE DRABINKI ---
        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetBracket(int tournamentId)
        {
            var matches = await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .Include(m => m.MatchResults) // Dołączamy wyniki, żeby frontend widział status pending
                .OrderBy(m => m.RoundNumber)
                .ThenBy(m => m.MatchNumber)
                .ToListAsync();

            if (!matches.Any()) return NotFound("Brak drabinki dla tego turnieju.");

            // Pobieramy nazwy uczestników
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

            // Mapowanie na DTO
            var matchDtos = matches.Select(m => {
                // Sprawdzamy, czy jest oczekujący wynik do wyświetlenia
                var pendingResult = m.MatchResults.FirstOrDefault(r => r.ResultStatus == "pending");

                return new MatchDto
                {
                    MatchId = m.MatchId,
                    MatchNumber = m.MatchNumber,
                    RoundNumber = m.RoundNumber,
                    MatchStatus = m.MatchStatus,
                    Score1 = (m.MatchStatus == "finished" && m.WinnerId.HasValue)
                             ? (m.WinnerId == m.Participant1Id ? 1 : 0) // Prosty score dla win/lose, można zmienić na realny z Result
                             : null,
                    Score2 = (m.MatchStatus == "finished" && m.WinnerId.HasValue)
                             ? (m.WinnerId == m.Participant2Id ? 1 : 0)
                             : null,

                    Participant1Id = m.Participant1Id,
                    Participant2Id = m.Participant2Id,
                    WinnerId = m.WinnerId,

                    Participant1Name = GetParticipantName(m.Participant1Id, m.Participant1Type, teams, users),
                    Participant2Name = GetParticipantName(m.Participant2Id, m.Participant2Type, teams, users),

                    // Dodajemy info o oczekującym wyniku dla frontendu
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

        private string GetParticipantName(int? id, string type, Dictionary<int, string> teams, Dictionary<int, string> users)
        {
            if (!id.HasValue) return "TBA";
            if (type == "team" && teams.ContainsKey(id.Value)) return teams[id.Value];
            if (type == "user" && users.ContainsKey(id.Value)) return users[id.Value];
            return "Unknown";
        }

        // --- USUWANIE DRABINKI (DODANE, ŻEBYŚ MÓGŁ NAPRAWIĆ BŁĄD) ---
        [HttpDelete("delete/{tournamentId}")]
        [Authorize(Roles = "admin,organizer")]
        public async Task<IActionResult> DeleteBracket(int tournamentId)
        {
            var matches = await _context.Matches.Where(m => m.TournamentId == tournamentId).ToListAsync();
            if (!matches.Any()) return NotFound("Brak drabinki do usunięcia.");

            // Usuń powiązane wyniki meczów
            var matchIds = matches.Select(m => m.MatchId).ToList();
            var results = await _context.MatchResults.Where(r => matchIds.Contains(r.MatchId)).ToListAsync();

            _context.MatchResults.RemoveRange(results);
            _context.Matches.RemoveRange(matches);

            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament != null) tournament.Status = "registration"; // Przywróć status rejestracji

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Drabinka została usunięta. Możesz wygenerować ją ponownie." });
        }

        // --- ZGŁASZANIE WYNIKU ---
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

            // Sprawdzenie uprawnień
            bool isParticipant = false;
            if (match.Participant1Type == "team")
            {
                isParticipant = await _context.Teams.AnyAsync(t => (t.TeamId == match.Participant1Id || t.TeamId == match.Participant2Id) && t.CaptainId == userId);
            }
            else
            {
                isParticipant = (match.Participant1Id == userId || match.Participant2Id == userId);
            }

            if (!isParticipant) return StatusCode(403, "Nie jesteś uczestnikiem tego meczu.");

            var existingReport = await _context.MatchResults
                .FirstOrDefaultAsync(r => r.MatchId == dto.MatchId && r.ResultStatus == "pending");

            if (existingReport != null) return BadRequest("Wynik został już zgłoszony i czeka na akceptację.");

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
            match.MatchStatus = "pending"; // Oznacz mecz jako oczekujący
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Wynik zgłoszony. Czekaj na potwierdzenie przeciwnika." });
        }

        // --- AKCEPTACJA WYNIKU ---
        [HttpPost("accept-result/{resultId}")]
        [Authorize]
        public async Task<IActionResult> AcceptResult(int resultId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var result = await _context.MatchResults.Include(r => r.Match).FirstOrDefaultAsync(r => r.ResultId == resultId);
            if (result == null) return NotFound("Nie znaleziono zgłoszenia.");
            if (result.ResultStatus != "pending") return BadRequest("Ten wynik nie oczekuje na potwierdzenie.");

            var match = result.Match;
            if (userId == result.ReportedBy) return BadRequest("Nie możesz zaakceptować własnego zgłoszenia!");

            // Walidacja uczestnika (uproszczona)
            bool isParticipant = false;
            if (match.Participant1Type == "team")
            {
                isParticipant = await _context.Teams.AnyAsync(t => (t.TeamId == match.Participant1Id || t.TeamId == match.Participant2Id) && t.CaptainId == userId);
            }
            else
            {
                isParticipant = (match.Participant1Id == userId || match.Participant2Id == userId);
            }
            if (!isParticipant) return StatusCode(403, "Brak uprawnień.");

            result.ResultStatus = "confirmed";
            result.ConfirmedBy = userId;
            result.ConfirmedAt = DateTime.UtcNow;

            await FinalizeMatchAndAdvance(match, result.Participant1Score, result.Participant2Score);
            return Ok(new { Message = "Wynik zaakceptowany!" });
        }

        // --- SPÓR ---
        [HttpPost("dispute-result/{resultId}")]
        [Authorize]
        public async Task<IActionResult> DisputeResult(int resultId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var result = await _context.MatchResults.Include(r => r.Match).FirstOrDefaultAsync(r => r.ResultId == resultId);
            if (result == null) return NotFound();

            result.ResultStatus = "disputed";
            result.Match.MatchStatus = "disputed";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Zgłoszono spór." });
        }

        // --- LOGIKA AWANSU ---
        private async Task FinalizeMatchAndAdvance(Match match, int scoreA, int scoreB)
        {
            match.MatchStatus = "finished";

            // Logika wyboru zwycięzcy
            int winnerId = (scoreA > scoreB) ? match.Participant1Id.Value : match.Participant2Id.Value;

            match.WinnerId = winnerId;
            match.WinnerType = match.Participant1Type;

            // Zapisz wynik w samym meczu (opcjonalnie, jeśli masz pola Score1/Score2 w modelu Match)
            // match.Score1 = scoreA; match.Score2 = scoreB; 

            // Szukanie meczu w następnej rundzie
            int nextRound = match.RoundNumber + 1;
            int nextMatchNum = (int)Math.Ceiling((double)match.MatchNumber / 2);

            var nextMatch = await _context.Matches
                .FirstOrDefaultAsync(m => m.TournamentId == match.TournamentId &&
                                          m.RoundNumber == nextRound &&
                                          m.MatchNumber == nextMatchNum);

            if (nextMatch != null)
            {
                // Jeśli jesteśmy nieparzystym numerem meczu (1, 3, 5) -> idziemy na slot 1
                bool isOddMatch = (match.MatchNumber % 2 != 0);

                if (isOddMatch)
                {
                    nextMatch.Participant1Id = winnerId;
                    nextMatch.Participant1Type = match.WinnerType;
                }
                else
                {
                    nextMatch.Participant2Id = winnerId;
                    nextMatch.Participant2Type = match.WinnerType;
                }
            }
            else
            {
                // Koniec turnieju
                var tournament = await _context.Tournaments.FindAsync(match.TournamentId);
                if (tournament != null) tournament.Status = "finished";
            }

            await _context.SaveChangesAsync();
        }
    }

    // DTOs
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

        public int? Score1 { get; set; }
        public int? Score2 { get; set; }

        public int? Participant1Id { get; set; }
        public string? Participant1Name { get; set; }

        public int? Participant2Id { get; set; }
        public string? Participant2Name { get; set; }

        public int? WinnerId { get; set; }

        public PendingResultDto? PendingResult { get; set; }
    }

    public class PendingResultDto
    {
        public int ResultId { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public int ReportedBy { get; set; }
    }
}