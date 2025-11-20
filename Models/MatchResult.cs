using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class MatchResult
    {
        [Key]
        public int ResultId { get; set; }

        [ForeignKey("Match")]
        public int MatchId { get; set; }
        public Match? Match { get; set; }

        public int Participant1Score { get; set; } = 0;
        public int Participant2Score { get; set; } = 0;

        [ForeignKey("Reporter")]
        public int ReportedBy { get; set; }
        public User? Reporter { get; set; }

        [ForeignKey("Confirmer")]
        public int? ConfirmedBy { get; set; }
        public User? Confirmer { get; set; } 

        [MaxLength(20)]
        public string ResultStatus { get; set; } = "pending"; 

        [MaxLength(255)]
        public string? ScreenshotUrl { get; set; }

        public string? Notes { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAt { get; set; }
    }
}