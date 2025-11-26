using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Nazwa użytkownika musi mieć od 3 do 50 znaków.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków.")]
        public string Password { get; set; } = string.Empty;

        
    }
}