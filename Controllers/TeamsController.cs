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

        [HttpPost("{teamId}/join")]
        [Authorize]
        public async Task<IActionResult> JoinTeam(int teamId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound("Nie znaleziono takiej drużyny.");

            var alreadyMember = await _context.TeamMembers
                .AnyAsync(m => m.TeamId == teamId && m.UserId == userId);

            if (alreadyMember)
            {
                return BadRequest("Już należysz do tej drużyny.");
            }
            var newMember = new TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Pomyślnie dołączyłeś do drużyny!" });
        }

        [HttpDelete("{teamId}/leave")]
        [Authorize]
        public async Task<IActionResult> LeaveTeam(int teamId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound("Nie znaleziono drużyny.");

            if (team.CaptainId == userId)
            {
                return BadRequest("Jesteś kapitanem! Nie możesz opuścić drużyny. Musisz ją usunąć lub przekazać dowodzenie.");
            }

            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId);

            if (member == null)
            {
                return BadRequest("Nie jesteś członkiem tej drużyny.");
            }
            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Opuściłeś drużynę." });
        }

        [HttpDelete("{teamId}/kick/{userIdToKick}")]
        [Authorize]
        public async Task<IActionResult> KickMember(int teamId, int userIdToKick)
        {
            var requesterIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(requesterIdString)) return Unauthorized();
            int requesterId = int.Parse(requesterIdString);
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound("Nie znaleziono drużyny.");

            if (team.CaptainId != requesterId)
            {
                return StatusCode(403, new { Message = "Tylko kapitan może wyrzucać graczy!" });
            }

            if (requesterId == userIdToKick)
            {
                return BadRequest("Nie możesz wyrzucić samego siebie. Użyj opcji 'Opuść drużynę'.");
            }

            var memberToKick = await _context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userIdToKick);

            if (memberToKick == null)
            {
                return NotFound("Ten użytkownik nie jest w Twojej drużynie.");
            }
            _context.TeamMembers.Remove(memberToKick);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Gracz został wyrzucony z drużyny." });
        }
    }
}