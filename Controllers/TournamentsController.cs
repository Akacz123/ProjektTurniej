using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

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
        public async Task<ActionResult<IEnumerable<Tournament>>> GetTournaments(string? status)
        {
            var query = _context.Tournaments
                .Include(t => t.Game)
                .Include(t => t.Organizer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            query = query.OrderBy(t => t.StartDate);

            return await query.ToListAsync();
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Tournament>> CreateTournament(Tournament tournament)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Brak ID użytkownika w tokenie");
            }
            tournament.OrganizerId = int.Parse(userIdString);
            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTournaments), new { id = tournament.TournamentId }, tournament);
        }
    }
}