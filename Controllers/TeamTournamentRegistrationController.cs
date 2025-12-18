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
    [Authorize]
    public class TeamTournamentRegistrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeamTournamentRegistrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register/{tournamentId}/{teamId}")]
        public async Task<IActionResult> RegisterTeam(int tournamentId, int teamId)
        {
            var memberCount = await _context.TeamMembers
                .CountAsync(m => m.TeamId == teamId && m.Status == "Member"); 
            var tournamentWithGame = await _context.Tournaments
                .Include(t => t.Game)
                .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

            if (tournamentWithGame?.Game != null)
            {
                if (memberCount < 1)
                {
                    return BadRequest("Twoja drużyna musi mieć członków, aby dołączyć!");
                }
            }
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("Brak ID użytkownika w tokenie.");
            }

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null)
            {
                return NotFound("Nie znaleziono takiej drużyny.");
            }

            if (team.CaptainId != userId)
            {
                return StatusCode(403, "Tylko kapitan może zapisać drużynę do turnieju!");
            }

            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                return NotFound("Nie znaleziono turnieju.");
            }

            if (tournament.Status != "registration")
            {
                return BadRequest("Rejestracja na ten turniej jest zamknięta.");
            }

            if (tournament.RegistrationType != "team")
            {
                return BadRequest("Ten turniej jest przeznaczony dla graczy indywidualnych.");
            }

            if (tournament.MaxParticipants.HasValue)
            {
                var currentTeamsCount = await _context.TournamentRegistrationsTeam
                    .CountAsync(r => r.TournamentId == tournamentId);

                if (currentTeamsCount >= tournament.MaxParticipants.Value)
                {
                    return BadRequest("Osiągnięto limit drużyn w tym turnieju.");
                }
            }

            var existingRegistration = await _context.TournamentRegistrationsTeam
                .FirstOrDefaultAsync(r => r.TournamentId == tournamentId && r.TeamId == teamId);

            if (existingRegistration != null)
            {
                return BadRequest("Twoja drużyna jest już zapisana na ten turniej.");
            }

            var registration = new TournamentRegistrationTeam
            {
                TournamentId = tournamentId,
                TeamId = teamId,
                RegisteredAt = DateTime.UtcNow,
                Status = "Confirmed"
            };

            _context.TournamentRegistrationsTeam.Add(registration);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Pomyślnie zapisano drużynę {team.TeamName} do turnieju!" });
        }

        [HttpGet("my-team-registrations")]
        public async Task<IActionResult> GetMyTeamRegistrations()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            var registrations = await _context.TournamentRegistrationsTeam
                .Include(r => r.Team)
                .Where(r => r.Team.CaptainId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.TournamentId,
                    TeamName = r.Team.TeamName,
                    r.RegisteredAt,
                    r.Status
                })
                .ToListAsync();

            return Ok(registrations);
        }
    }
}