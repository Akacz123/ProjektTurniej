using System.Security.Claims;
using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using EsportsTournament.API.Models.DTOs;
using EsportsTournament.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektTurniej.Services;

namespace EsportsTournament.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, PasswordService passwordService, JwtService jwtService)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            {
                return BadRequest(new { Message = "Użytkownik o podanej nazwie lub e-mailu już istnieje." });
            }

            string passwordHash = _passwordService.HashPassword(request.Password);

            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "user",
                FirstName = request.FirstName,
                LastName = request.LastName,
                AvatarUrl = request.AvatarUrl
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Rejestracja pomyślna." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return Unauthorized(new { Message = "Nieprawidłowa nazwa użytkownika lub hasło." });
            }

            bool isPasswordValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new { Message = "Nieprawidłowa nazwa użytkownika lub hasło." });
            }

            string token = _jwtService.GenerateToken(user.UserId, user.Username, user.Role);

            return Ok(new { Token = token, Username = user.Username, Role = user.Role });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(new { Message = "Błąd tokena." });
            }

            int userId = int.Parse(userIdClaim.Value);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { Message = "Użytkownik nie został znaleziony." });
            }

            return Ok(new
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive
            });
        }
    }
}