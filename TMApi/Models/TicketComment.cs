using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TMApi.Models
{
    public class TicketComment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Content { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;  

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        // Foreign key to Ticket
        [Required]
        public int TicketId { get; set; }

        [Required]
        public string UserId { get; set; }

        [JsonIgnore]
        public ApplicationUser? User { get; set; }

        [JsonIgnore]
        public Ticket? Ticket { get; set; }


    }
}
