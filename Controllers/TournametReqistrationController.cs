using EsportsTournament.API.Data;
using EsportsTournament.API.Models;
using EsportsTournament.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EsportsTournament.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TournamentRegistrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TournamentRegistrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Nie można odczytać ID użytkownika z tokena. Upewnij się, że jesteś zalogowany.");
            }
            return userId;
        }

        [HttpGet("my-tournaments")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<TournamentRegistrationDto>))]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetMyRegisteredTournaments()
        {
            try
            {
                var userId = GetUserIdFromToken();

                var registeredTournaments = await _context.TournamentRegistrationIndividual
                    .Where(r => r.UserId == userId)
                    .Select(r => new TournamentRegistrationDto
                    {
                        TournamentId = r.TournamentId,
                        TournamentName = r.Tournament!.TournamentName,
                        RegistrationType = r.Tournament.RegistrationType,
                        StartDate = r.Tournament.StartDate,
                        Status = r.Tournament.Status,
                        RegisteredAt = r.RegisteredAt
                    })
                    .ToListAsync();

                return Ok(registeredTournaments);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Wystąpił błąd serwera podczas pobierania turniejów.", Detail = ex.Message });
            }
        }

        [HttpPost("{tournamentId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RegisterForTournament(int tournamentId)
        {
            try
            {
                var userId = GetUserIdFromToken();

                var tournament = await _context.Tournaments
                    .FirstOrDefaultAsync(t => t.TournamentId == tournamentId);

                if (tournament == null)
                {
                    return NotFound(new { Message = $"Turniej o ID {tournamentId} nie został znaleziony." });
                }

                if (tournament.Status != "registration")
                {
                    return BadRequest(new { Message = "Rejestracja do tego turnieju jest już zamknięta lub zakończona." });
                }

                if (tournament.RegistrationType != "individual")
                {
                    return BadRequest(new { Message = "Ten turniej wymaga rejestracji drużynowej. Użyj odpowiedniego endpointu." });
                }

                bool alreadyRegistered = await _context.TournamentRegistrationIndividual
                    .AnyAsync(r => r.TournamentId == tournamentId && r.UserId == userId);

                if (alreadyRegistered)
                {
                    return Conflict(new { Message = "Jesteś już zarejestrowany w tym turnieju." });
                }

                if (tournament.MaxParticipants.HasValue)
                {
                    var currentParticipants = await _context.TournamentRegistrationIndividual
                        .CountAsync(r => r.TournamentId == tournamentId);

                    if (currentParticipants >= tournament.MaxParticipants.Value)
                    {
                        return BadRequest(new { Message = "Osiągnięto maksymalną liczbę uczestników turnieju. Rejestracja niemożliwa." });
                    }
                }

                var newRegistration = new TournamentRegistrationIndividual
                {
                    TournamentId = tournamentId,
                    UserId = userId,
                    RegisteredAt = DateTime.UtcNow,
                    Status = "confirmed"
                };

                await _context.TournamentRegistrationIndividual.AddAsync(newRegistration);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Pomyślnie zarejestrowano w turnieju.", RegistrationId = newRegistration.RegistrationId });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Wystąpił błąd serwera podczas rejestracji w turnieju.", Detail = ex.Message });
            }
        }
    }
}