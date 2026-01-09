namespace EsportsTournament.API.Models.DTOs
{
    public class TournamentDto
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public int GameId { get; set; }
        public string? GameName { get; set; }
        public int OrganizerId { get; set; }
        public string? OrganizerName { get; set; }
        public string? Description { get; set; }
        public string? Rules { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxParticipants { get; set; }

        public string TournamentFormat { get; set; } = string.Empty;
        public string RegistrationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public int ParticipantsCount { get; set; }
    }
}