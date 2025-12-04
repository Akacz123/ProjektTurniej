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

            if (team == null) return NotFound();
            return team;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Team>> CreateTeam(Team team)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            if (await _context.Teams.AnyAsync(t => t.TeamName == team.TeamName))
                return BadRequest("Drużyna o takiej nazwie już istnieje.");

            team.CaptainId = userId;
            team.CreatedAt = DateTime.UtcNow;
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var member = new TeamMember
            {
                TeamId = team.TeamId,
                UserId = userId,
                Role = "Captain",
                Status = "Member",
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
            string username = User.FindFirst("username")?.Value ?? "Ktoś";

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound("Nie znaleziono drużyny.");

            var alreadyMember = await _context.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId);
            if (alreadyMember) return BadRequest("Już należysz do tej drużyny (lub czekasz na akceptację).");

            var newMember = new TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                Role = "Member",
                Status = "Pending", 
                JoinedAt = DateTime.UtcNow
            };
            _context.TeamMembers.Add(newMember);

            var notification = new Notification
            {
                UserId = team.CaptainId,
                Title = "Nowe zgłoszenie do drużyny",
                Message = $"Gracz {username} chce dołączyć do Twojej drużyny {team.TeamName}.",
                NotificationType = "TeamJoinRequest",
                RelatedId = teamId,
                RelatedType = "Team"
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Wysłano prośbę o dołączenie do kapitana." });
        }

        [HttpPost("{teamId}/approve/{userIdToApprove}")]
        [Authorize]
        public async Task<IActionResult> ApproveMember(int teamId, int userIdToApprove)
        {
            var captainIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(captainIdString)) return Unauthorized();
            int captainId = int.Parse(captainIdString);

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound("Nie ma takiej drużyny.");
            if (team.CaptainId != captainId) return StatusCode(403, "Tylko kapitan może akceptować członków.");

            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userIdToApprove && m.Status == "Pending");

            if (member == null) return NotFound("Nie znaleziono oczekującego zgłoszenia od tego gracza.");

            member.Status = "Member";

            _context.Notifications.Add(new Notification
            {
                UserId = userIdToApprove,
                Title = "Zgłoszenie przyjęte!",
                Message = $"Zostałeś przyjęty do drużyny {team.TeamName}.",
                NotificationType = "TeamJoinAccepted"
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Gracz został przyjęty do drużyny." });
        }

        [HttpPost("{teamId}/invite/{friendId}")]
        [Authorize]
        public async Task<IActionResult> InviteFriend(int teamId, int friendId)
        {
            var captainIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(captainIdString)) return Unauthorized();
            int captainId = int.Parse(captainIdString);

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound("Nie ma takiej drużyny.");
            if (team.CaptainId != captainId) return StatusCode(403, "Tylko kapitan może zapraszać.");

            var areFriends = await _context.Friendships
                .AnyAsync(f => (f.RequesterId == captainId && f.AddresseeId == friendId && f.Status == "Accepted") ||
                               (f.RequesterId == friendId && f.AddresseeId == captainId && f.Status == "Accepted"));

            if (!areFriends)
            {
                return BadRequest("Możesz zapraszać tylko swoich znajomych!");
            }

            if (await _context.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == friendId))
                return BadRequest("Ten gracz już jest w drużynie lub został zaproszony.");

  
            _context.Notifications.Add(new Notification
            {
                UserId = friendId,
                Title = "Zaproszenie do drużyny",
                Message = $"Kapitan {team.TeamName} zaprasza Cię do składu.",
                NotificationType = "TeamInvite",
                RelatedId = teamId,
                RelatedType = "Team"
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Zaproszenie wysłane do znajomego." });
        }
    }
}