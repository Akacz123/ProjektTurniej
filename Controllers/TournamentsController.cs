using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace EsportsTournament.API.Controllers
{
    [Route("api/[controller]")] // To ustala adres na: /api/tournaments
    [ApiController]
    public class TournamentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Konstruktor: Tutaj "wstrzykujemy" połączenie do bazy danych
        public TournamentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tournament>>> GetTournaments(string? status)
        {
            var query = _context.Tournaments
                .Include(t => t.Game)
                .AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }
            query = query.OrderBy(t => t.StartDate);
            return await query.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Tournament>> CreateTournament(Tournament tournament)
        {
            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTournaments), new { id = tournament.TournamentId }, tournament);
        }
    }
}