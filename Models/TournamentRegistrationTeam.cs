using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class TournamentRegistrationTeam
    {
        [Key]
        public int RegistrationId { get; set; }

        [ForeignKey("Tournament")]
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        [ForeignKey("Team")]
        public int TeamId { get; set; }
        public Team? Team { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "confirmed";
    }
}