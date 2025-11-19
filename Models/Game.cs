using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models
{
    public class Game
    {
        [Key]
        public int GameId { get; set; }

        [Required]
        [MaxLength(100)]
        public string GameName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? GameIconUrl { get; set; }

        public string? Description { get; set; }

        public int MaxTeamSize { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}