using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    [Table("tournament_registrations_individual")]
    public class TournamentRegistrationIndividual
    {
        [Key]
        public int RegistrationId { get; set; }

        [ForeignKey("Tournament")]
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "confirmed";
    }
}