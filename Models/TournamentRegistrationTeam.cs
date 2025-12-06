using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class TournamentRegistrationTeam
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }

        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Confirmed";
    }
}