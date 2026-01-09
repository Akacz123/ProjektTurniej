using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models.DTOs
{
    
    public class MatchDto
    {
        public int MatchId { get; set; }
        public int MatchNumber { get; set; }
        public int RoundNumber { get; set; }

        public string MatchStatus { get; set; } = string.Empty;

        public int? Score1 { get; set; }
        public int? Score2 { get; set; }

        public int? Participant1Id { get; set; }
        public string? Participant1Name { get; set; }

        public int? Participant2Id { get; set; }
        public string? Participant2Name { get; set; }

        public int? WinnerId { get; set; }

        public PendingResultDto? PendingResult { get; set; }
    }
    public class MatchResultDto
    {
        public int MatchId { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public string? ScreenshotUrl { get; set; }
    }

    public class PendingResultDto
    {
        public int ResultId { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
        public int ReportedBy { get; set; }
    }
}