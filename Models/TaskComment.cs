using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TMApi.Models
{
    public class TaskComment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Content { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;  

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        // Foreign key to TaskItem
        [Required]
        public int TaskId { get; set; }

        [Required]
        public string UserId { get; set; }

        [JsonIgnore]
        public ApplicationUser? User { get; set; }

        [JsonIgnore]
        public TaskItem? Task { get; set; }


    }
}
