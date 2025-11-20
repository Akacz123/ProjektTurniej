using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class UserReport
    {
        [Key]
        public int ReportId { get; set; }

        [ForeignKey("Reporter")]
        public int ReporterId { get; set; }
        public User? Reporter { get; set; }

        [ForeignKey("ReportedUser")]
        public int ReportedUserId { get; set; }
        public User? ReportedUser { get; set; }

        [Required]
        [MaxLength(30)]
        public string Reason { get; set; } = string.Empty;

        public string? Description { get; set; }

        [ForeignKey("Match")]
        public int? MatchId { get; set; }
        public Match? Match { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}