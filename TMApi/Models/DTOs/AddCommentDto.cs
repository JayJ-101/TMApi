using System.ComponentModel.DataAnnotations;

namespace TMApi.Models  
{
    public class AddCommentDto
    {
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty; 
        public bool IsInternal { get; set; } = false;
    }
}
