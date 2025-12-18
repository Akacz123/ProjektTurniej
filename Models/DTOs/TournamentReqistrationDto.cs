namespace EsportsTournament.API.Models.DTOs
{
    public class TournamentRegistrationDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public string RegistrationType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}