using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models // lub .DTOs
{
    public class UserUpdateDto
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string AvatarUrl { get; set; } = string.Empty;
    }
}