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
            return await _context.Teams
                .Include(t => t.Captain)
                .Include(t => t.TeamMembers)
                .ThenInclude(tm => tm.User)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Team>> GetTeam(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Captain)
                .Include(t => t.TeamMembers)
                .ThenInclude(tm => tm.User)
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

        [HttpGet("avatars")]
        public async Task<ActionResult<IEnumerable<TeamAvatar>>> GetTeamAvatars()
        {
            return await _context.TeamAvatars.ToListAsync();
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

            var existingMember = await _context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId);

            if (existingMember != null && existingMember.Status == "Pending")
            {
                existingMember.Status = "Member";
                existingMember.JoinedAt = DateTime.UtcNow;

                _context.Notifications.Add(new Notification
                {
                    UserId = team.CaptainId,
                    Title = "Zaproszenie przyjęte",
                    Message = $"Gracz {username} dołączył do Twojej drużyny.",
                    NotificationType = "Info",
                    RelatedId = teamId
                });

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Pomyślnie dołączyłeś do drużyny!" });
            }

            if (existingMember != null && existingMember.Status == "Member")
            {
                return BadRequest("Już należysz do tej drużyny.");
            }

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
                Message = $"Gracz {username} chce dołączyć do Twojej drużyny '{team.TeamName}'.",
                NotificationType = "TeamJoinRequest",
                RelatedId = teamId,
                RelatedType = "Team",
                RelatedUserId = userId
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

            var newMember = new TeamMember
            {
                TeamId = teamId,
                UserId = friendId,
                Role = "Member",
                Status = "Pending",
                JoinedAt = DateTime.UtcNow
            };
            _context.TeamMembers.Add(newMember);

            _context.Notifications.Add(new Notification
            {
                UserId = friendId,
                Title = "Zaproszenie do drużyny",
                Message = $"Kapitan drużyny '{team.TeamName}' zaprasza Cię do składu.",
                NotificationType = "TeamInvite",
                RelatedId = teamId,
                RelatedType = "Team"
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Zaproszenie wysłane do znajomego." });
        }

        [HttpPost("{teamId}/reject")]
        [Authorize]
        public async Task<IActionResult> RejectInvite(int teamId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId && m.Status == "Pending");

            if (member == null) return NotFound("Nie znaleziono zaproszenia do odrzucenia.");

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Zaproszenie odrzucone." });
        }

        [HttpDelete("{teamId}/leave")]
        [Authorize]
        public async Task<IActionResult> LeaveTeam(int teamId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var team = await _context.Teams
                .Include(t => t.TeamMembers)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null) return NotFound("Nie znaleziono drużyny.");

            var member = team.TeamMembers.FirstOrDefault(m => m.UserId == userId);
            if (member == null) return NotFound("Nie należysz do tej drużyny.");

            if (member.Role == "Captain")
            {
                if (team.TeamMembers.Count > 1)
                {
                    return BadRequest("Kapitan nie może opuścić drużyny, gdy są w niej inni gracze. Wyrzuć ich najpierw lub przekaż rolę.");
                }

                var relatedNotifications = await _context.Notifications
                    .Where(n => n.RelatedType == "Team" && n.RelatedId == teamId)
                    .ToListAsync();
                _context.Notifications.RemoveRange(relatedNotifications);

                _context.TeamMembers.RemoveRange(team.TeamMembers);
                _context.Teams.Remove(team);

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Drużyna została rozwiązana, ponieważ byłeś jedynym członkiem." });
            }

            _context.TeamMembers.Remove(member);

            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Użytkownik";
            _context.Notifications.Add(new Notification
            {
                UserId = team.CaptainId,
                Title = "Gracz opuścił drużynę",
                Message = $"Gracz {username} opuścił Twoją drużynę {team.TeamName}.",
                NotificationType = "Info",
                RelatedId = teamId
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Opuściłeś drużynę." });
        }

        [HttpDelete("{teamId}/kick/{userIdToKick}")]
        [Authorize]
        public async Task<IActionResult> KickMember(int teamId, int userIdToKick)
        {
            var captainIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(captainIdString)) return Unauthorized();
            int captainId = int.Parse(captainIdString);

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound("Nie ma takiej drużyny.");

            if (team.CaptainId != captainId) return StatusCode(403, "Tylko kapitan może wyrzucać graczy.");
            if (userIdToKick == captainId) return BadRequest("Nie możesz wyrzucić samego siebie.");

            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userIdToKick);

            if (member == null) return NotFound("Ten użytkownik nie jest w Twojej drużynie.");

            _context.TeamMembers.Remove(member);

            _context.Notifications.Add(new Notification
            {
                UserId = userIdToKick,
                Title = "Zostałeś wyrzucony",
                Message = $"Zostałeś usunięty z drużyny {team.TeamName} przez kapitana.",
                NotificationType = "Info",
                RelatedId = teamId
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Gracz został wyrzucony z drużyny." });
        }

        [HttpDelete("{teamId}")]
        [Authorize]
        public async Task<IActionResult> DeleteTeam(int teamId)
        {
            var captainIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(captainIdString)) return Unauthorized();
            int captainId = int.Parse(captainIdString);

            var team = await _context.Teams
                .Include(t => t.TeamMembers)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null) return NotFound("Nie znaleziono drużyny.");
            if (team.CaptainId != captainId) return StatusCode(403, "Tylko kapitan może usunąć drużynę.");

            var membersCount = team.TeamMembers.Count;

            if (membersCount > 1)
            {
                return BadRequest("Nie możesz usunąć drużyny, dopóki są w niej inni gracze.");
            }

            var relatedNotifications = await _context.Notifications
                .Where(n => n.RelatedType == "Team" && n.RelatedId == teamId)
                .ToListAsync();
            _context.Notifications.RemoveRange(relatedNotifications);

            _context.TeamMembers.RemoveRange(team.TeamMembers);
            _context.Teams.Remove(team);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Drużyna została usunięta." });
        }
    }
}