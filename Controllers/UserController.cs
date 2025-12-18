using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using EsportsTournament.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsportsTournament.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto request)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            int tokenUserId = int.Parse(userIdString);

            if (tokenUserId != id)
            {
                return StatusCode(403, "Nie możesz edytować cudzego profilu!");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Użytkownik nie istnieje.");

            if (request.Username != user.Username)
            {
                if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                {
                    return BadRequest("Ta nazwa użytkownika jest już zajęta.");
                }
            }
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest("Ten email jest już zajęty.");
                }
            }

            user.Username = request.Username;
            user.Email = request.Email;
            user.AvatarUrl = request.AvatarUrl;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id)) return NotFound();
                else throw;
            }

            return Ok(new
            {
                Message = "Profil zaktualizowany!",
                User = new { user.UserId, user.Username, user.Email, user.AvatarUrl }
            });
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}