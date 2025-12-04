using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class TeamMember
    {
        [Key]
        public int MemberId { get; set; }

        [ForeignKey("Team")]
        public int TeamId { get; set; }
        public Team? Team { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "member";
        [MaxLength(20)]
        public string Status { get; set; } = "Member"; 

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}