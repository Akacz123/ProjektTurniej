using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsportsTournament.API.Models
{
    public class Friendship
    {
        [Key]
        public int FriendshipId { get; set; }

        [ForeignKey("Requester")]
        public int RequesterId { get; set; }
        public User? Requester { get; set; }

        [ForeignKey("Addressee")]
        public int AddresseeId { get; set; }
        public User? Addressee { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}