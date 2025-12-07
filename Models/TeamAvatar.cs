using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models
{
    public class TeamAvatar
    {
        [Key]
        public int TeamAvatarId { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;
    }
}