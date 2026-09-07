using Microsoft.AspNetCore.Identity;

namespace TMApi.Models
{
    public class ApplicationUser :IdentityUser
    {
        public string? FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public DateTime? LastLogin { get; set; }


    }
}

