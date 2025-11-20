using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class TeamStatistic
    {
        [Key]
        public int StatId { get; set; }

        [ForeignKey("Team")]
        public int TeamId { get; set; }
        public Team? Team { get; set; }

        [ForeignKey("Game")]
        public int GameId { get; set; }
        public Game? Game { get; set; }

        public int MatchesPlayed { get; set; } = 0;
        public int MatchesWon { get; set; } = 0;
        public int MatchesLost { get; set; } = 0;
        public int TournamentsParticipated { get; set; } = 0;
        public int TournamentsWon { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}