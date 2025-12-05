using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class Team
    {
        [Key]
        public int TeamId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [ForeignKey("Captain")]
        public int CaptainId { get; set; }
        public User? Captain { get; set; }

        [MaxLength(255)]
        public string? LogoUrl { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    }
}