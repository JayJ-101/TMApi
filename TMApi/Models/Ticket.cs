using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TMApi.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public TicketCategory Category { get; set; } = TicketCategory.General;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        //Foreing key
        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public string? AssignedUserId { get; set; }
        public string? ResolvedByUserId { get; set; }
        public string? ClosedByUserId { get; set; }

        [JsonIgnore]
        public ApplicationUser? CreatedByUser { get; set; }
        [JsonIgnore]
        public ApplicationUser? AssignedUser { get; set; }
        [JsonIgnore]
        public ApplicationUser? ResolvedByUser { get; set; }
        [JsonIgnore]
        public ApplicationUser? ClosedByUser { get; set; }
    }

    public enum TicketStatus
    {
        Open = 0,
        InProgress = 1,
        Resolved = 2,
        Closed = 3,
        Reopened = 4
    }

    public enum TicketPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public enum TicketCategory
    {
        General = 0,
        Hardware = 1,
        Software = 2,
        Network = 3,
        Access = 4,
        Email = 5,
        Security = 6,
        Billing = 7,
        Other = 8
    }

}
