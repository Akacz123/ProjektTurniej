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
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetMyNotifications()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int userId = int.Parse(userIdString);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int userId = int.Parse(userIdString!);

            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null) return NotFound();
            if (notification.UserId != userId) return Unauthorized();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("readAll")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int userId = int.Parse(userIdString!);

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Wszystkie powiadomienia oznaczone jako przeczytane." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int userId = int.Parse(userIdString!);

            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null) return NotFound();

            if (notification.UserId != userId) return Unauthorized();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Powiadomienie usunięte." });
        }
    }
}