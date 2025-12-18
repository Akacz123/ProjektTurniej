using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class TournamentRegistration
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }
        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; } 

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Approved";
    }
}