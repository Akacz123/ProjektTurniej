using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail.")]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Email musi mieć format np. jan@domena.pl")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        public string Password { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Imię nie może być dłuższe niż 50 znaków.")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Nazwisko nie może być dłuższe niż 50 znaków.")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Link do avatara jest za długi.")]
        public string? AvatarUrl { get; set; }
    }
}