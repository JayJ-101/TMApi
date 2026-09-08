using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TMApi.Models
{
    public class TaskAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

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
