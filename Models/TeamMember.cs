using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EsportsTournament.API.Models
{
    public class TeamMember
    {
        [Key]
        public int Id { get; set; }

        public int TeamId { get; set; }

        [JsonIgnore]
        public Team? Team { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string Role { get; set; } = "Member";
        public string Status { get; set; } = "Pending";
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}