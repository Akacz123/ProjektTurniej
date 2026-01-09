using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using EsportsTournament.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EsportsTournament.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TournamentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TournamentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TournamentDto>>> GetTournaments(string? status)
        {
            var query = _context.Tournaments
                .Include(t => t.Game)
                .Include(t => t.Organizer)
                .AsQueryable();

            var now = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "upcoming":
                        query = query.Where(t => t.StartDate > now).OrderBy(t => t.StartDate);
                        break;
                    case "ongoing":
                        query = query.Where(t => t.StartDate <= now && t.EndDate > now);
                        break;
                    case "finished":
                        query = query.Where(t => t.EndDate <= now).OrderByDescending(t => t.EndDate);
                        break;
                    default:
                        query = query.Where(t => t.Status == status);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(t => t.StartDate);
            }

            var tournaments = await query.ToListAsync();
            var result = new List<TournamentDto>();

            foreach (var t in tournaments)
            {
                int count = 0;
                if (t.RegistrationType?.ToLower() == "team")
                {
                    count = await _context.TournamentRegistrationsTeam
                        .CountAsync(r => r.TournamentId == t.TournamentId && r.Status == "Confirmed");
                }
                else
                {
                    count = await _context.TournamentRegistrationIndividual
                        .CountAsync(r => r.TournamentId == t.TournamentId && r.Status.ToLower() == "confirmed");
                }

                result.Add(new TournamentDto
                {
                    TournamentId = t.TournamentId,
                    TournamentName = t.TournamentName,
                    GameId = t.GameId,
                    GameName = t.Game?.GameName,
                    OrganizerId = t.OrganizerId ?? 0,
                    OrganizerName = t.Organizer?.Username,
                    Description = t.Description,
                    Rules = t.Rules,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    MaxParticipants = t.MaxParticipants,
                    TournamentFormat = t.TournamentFormat ?? string.Empty,
                    RegistrationType = t.RegistrationType ?? string.Empty,
                    Status = t.Status ?? string.Empty,
                    ImageUrl = t.ImageUrl,
                    ParticipantsCount = count
                });
            }

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Tournament>> CreateTournament(Tournament tournament)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Brak ID użytkownika w tokenie");
            }
            tournament.OrganizerId = int.Parse(userIdString);
            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTournaments), new { id = tournament.TournamentId }, tournament);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTournament(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

            if (string.IsNullOrEmpty(userRole) || userRole != "admin")
            {
                return StatusCode(403, "Tylko administrator może usuwać turnieje.");
            }

            var tournament = await _context.Tournaments.FindAsync(id);

            if (tournament == null)
            {
                return NotFound("Nie znaleziono turnieju.");
            }

            var individualRegistrations = _context.TournamentRegistrationIndividual.Where(r => r.TournamentId == id);
            _context.TournamentRegistrationIndividual.RemoveRange(individualRegistrations);

            var teamRegistrations = _context.TournamentRegistrationsTeam.Where(r => r.TournamentId == id);
            _context.TournamentRegistrationsTeam.RemoveRange(teamRegistrations);

            var genericRegistrations = _context.TournamentRegistrations.Where(r => r.TournamentId == id);
            _context.TournamentRegistrations.RemoveRange(genericRegistrations);

            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTournament(int id, Tournament tournament)
        {
            if (id != tournament.TournamentId)
            {
                return BadRequest("ID turnieju w URL nie zgadza się z ID w ciele żądania.");
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            if (string.IsNullOrEmpty(userRole) || userRole != "admin")
            {
                return StatusCode(403, "Tylko administrator może edytować wszystkie dane turnieju.");
            }

            var existingTournament = await _context.Tournaments.FindAsync(id);
            if (existingTournament == null)
            {
                return NotFound("Nie znaleziono turnieju.");
            }

            existingTournament.TournamentName = tournament.TournamentName;
            existingTournament.GameId = tournament.GameId;
            existingTournament.OrganizerId = tournament.OrganizerId;
            existingTournament.Description = tournament.Description;
            existingTournament.Rules = tournament.Rules;
            existingTournament.StartDate = tournament.StartDate;
            existingTournament.EndDate = tournament.EndDate;
            existingTournament.MaxParticipants = tournament.MaxParticipants;
            existingTournament.TournamentFormat = tournament.TournamentFormat;
            existingTournament.RegistrationType = tournament.RegistrationType;
            existingTournament.Status = tournament.Status;
            existingTournament.ImageUrl = tournament.ImageUrl;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Tournaments.AnyAsync(e => e.TournamentId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
    }
}