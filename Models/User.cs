using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class User
    {
        [Key] // To oznacza Primary Key
        public int UserId { get; set; }

        [Required] // Not Null
        [MaxLength(50)] // Varchar(50)
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress] // Dodatkowa walidacja formatu email
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? FirstName { get; set; } // Znak zapytania oznacza, że może być Null

        [MaxLength(50)]
        public string? LastName { get; set; }

        [MaxLength(20)]
        public string Role { get; set; } = "user"; // Domyślna wartość

        [MaxLength(255)]
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Domyślnie aktualny czas

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true; // Domyślnie 1 (true)
    }
}