using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        public string Username { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        public string Password { get; set; } = string.Empty;
    }
}