using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryPro.Web.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string? UserId { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Action { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Controller { get; set; }

        [MaxLength(100)]
        public string? ActionName { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? EntityId { get; set; }

        [MaxLength(100)]
        public string? EntityType { get; set; }

        // Old and new values for tracking changes
        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [Required]
        [MaxLength(50)]
        public string? OperationType { get; set; } // Create, Read, Update, Delete, Login, Logout, etc.

        public bool IsSuccess { get; set; } = true;

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        [NotMapped]
        public string? FormattedTimestamp => Timestamp.ToString("dd MMM yyyy HH:mm:ss");

        [NotMapped]
        public string? FriendlyDescription => string.IsNullOrWhiteSpace(Description)
            ? $"{OperationType ?? "Activity"} recorded for {EntityType ?? Controller ?? "the system"}."
            : Description;
    }
}
