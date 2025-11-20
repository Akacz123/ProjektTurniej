using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class Match
    {
        [Key]
        public int MatchId { get; set; }

        [ForeignKey("Tournament")]
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        public int RoundNumber { get; set; }
        public int MatchNumber { get; set; }

        public int? Participant1Id { get; set; }
        public int? Participant2Id { get; set; }

        [MaxLength(10)]
        public string Participant1Type { get; set; } = "team";

        [MaxLength(10)]
        public string Participant2Type { get; set; } = "team";

        public int? WinnerId { get; set; }

        [MaxLength(10)]
        public string? WinnerType { get; set; }

        public DateTime? ScheduledTime { get; set; }

        [MaxLength(20)]
        public string MatchStatus { get; set; } = "pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}