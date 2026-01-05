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
    public class FriendsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FriendsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("invite/{targetUserId}")]
        public async Task<IActionResult> SendInvite(int targetUserId)
        {
            var myIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(myIdString)) return Unauthorized();
            int myId = int.Parse(myIdString);

            if (myId == targetUserId) return BadRequest("Nie możesz zaprosić samego siebie.");

            var existing = await _context.Friendships
                .AnyAsync(f => (f.RequesterId == myId && f.AddresseeId == targetUserId) ||
                               (f.RequesterId == targetUserId && f.AddresseeId == myId));

            if (existing) return BadRequest("Zaproszenie już wysłano lub jesteście znajomymi.");

            var friendship = new Friendship
            {
                RequesterId = myId,
                AddresseeId = targetUserId,
                Status = "Pending"
            };
            _context.Friendships.Add(friendship);

            var senderName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Ktoś";
            var notification = new Notification
            {
                UserId = targetUserId,
                Title = "Nowe zaproszenie do znajomych",
                Message = $"Użytkownik {senderName} (ID: {myId}) chce dodać Cię do znajomych.",
                NotificationType = "FriendRequest",
                RelatedType = "User",
                RelatedId = myId
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Zaproszenie wysłane!" });
        }

        [HttpPost("accept/{requesterId}")]
        public async Task<IActionResult> AcceptInvite(int requesterId)
        {
            var myId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == myId && f.Status == "Pending");

            if (friendship == null) return NotFound("Nie znaleziono takiego zaproszenia.");
            friendship.Status = "Accepted";

            var acceptingUser = await _context.Users.FindAsync(myId);
            string acceptingUserName = acceptingUser?.Username ?? "Unknown User";

            _context.Notifications.Add(new Notification
            {
                UserId = requesterId,
                Title = "Zaproszenie przyjęte",
                Message = $"Użytkownik {acceptingUserName} zaakceptował Twoje zaproszenie.", // Use Name, not ID
                NotificationType = "FriendRequestAccepted",
                RelatedId = myId,
                RelatedType = "User"
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Jesteście teraz znajomymi!" });
        }

        [HttpDelete("remove/{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var myIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(myIdString)) return Unauthorized();
            int myId = int.Parse(myIdString);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == myId && f.AddresseeId == friendId) ||
                    (f.RequesterId == friendId && f.AddresseeId == myId));

            if (friendship == null)
            {
                return NotFound("Nie znaleziono takiej znajomości lub zaproszenia.");
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Użytkownik został usunięty ze znajomych." });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMyFriends()
        {
            var myId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);

            var friends = await _context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => (f.RequesterId == myId || f.AddresseeId == myId) && f.Status == "Accepted")
                .ToListAsync();

            var friendList = friends.Select(f =>
                f.RequesterId == myId ? f.Addressee : f.Requester
            ).Select(u => new
            {
                u!.UserId,
                u.Username,
                u.AvatarUrl,
                u.IsActive
            });

            return Ok(friendList);
        }

        [HttpGet("requests")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingRequests()
        {
            var myId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);

            var requests = await _context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == myId && f.Status == "Pending")
                .Select(f => new
                {
                    RequestId = f.FriendshipId,
                    SenderId = f.Requester!.UserId,
                    SenderName = f.Requester.Username,
                    SentAt = f.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }
    }
}