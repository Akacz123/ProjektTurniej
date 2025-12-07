using System.ComponentModel.DataAnnotations;

namespace EsportsTournament.API.Models
{
    public class PresetAvatar
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}