using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class Tournament
    {
        [Key]
        public int TournamentId { get; set; }

        [Required]
        [MaxLength(150)]
        public string TournamentName { get; set; } = string.Empty;

        // Klucz obcy do Game
        [ForeignKey("Game")]
        public int GameId { get; set; }
        public Game? Game { get; set; } // To pozwala łatwo pobrać dane gry dla turnieju

        // Klucz obcy do User (Organizator)
        [ForeignKey("Organizer")]
        public int? OrganizerId { get; set; }
        public User? Organizer { get; set; }

        public string? Description { get; set; }
        public string? Rules { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? MaxParticipants { get; set; }

        [MaxLength(30)]
        public string TournamentFormat { get; set; } = "single_elimination";

        [MaxLength(20)]
        public string RegistrationType { get; set; } = "team";

        [MaxLength(20)]
        public string Status { get; set; } = "registration";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ImageUrl { get; set; }
    }
}