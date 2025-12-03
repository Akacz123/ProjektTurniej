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
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Team>>> GetTeams()
        {
            return await _context.Teams.Include(t => t.Captain).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Team>> GetTeam(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Captain)
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
            {
                return NotFound();
            }

            return team;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Team>> CreateTeam(Team team)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                userIdString = User.FindFirst("sub")?.Value;
            }

            if (string.IsNullOrEmpty(userIdString))
            {
                userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            }

            if (string.IsNullOrEmpty(userIdString))
            {
                var claims = string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"));
                return Unauthorized($"Błąd tokena. Nie znaleziono ID. Dostępne claimy: {claims}");
            }

            int userId = int.Parse(userIdString);

            if (await _context.Teams.AnyAsync(t => t.TeamName == team.TeamName))
            {
                return BadRequest("Drużyna o takiej nazwie już istnieje.");
            }

            team.CaptainId = userId;
            team.CreatedAt = DateTime.UtcNow;

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var member = new TeamMember
            {
                TeamId = team.TeamId,
                UserId = userId,
                Role = "Captain",
                JoinedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeam), new { id = team.TeamId }, team);
        }
    }
}