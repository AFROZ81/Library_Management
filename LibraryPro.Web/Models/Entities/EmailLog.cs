using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryPro.Web.Models.Entities
{
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string? ToEmail { get; set; }

        [Required]
        [MaxLength(500)]
        public string? Subject { get; set; }

        [Required]
        public string? Body { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsSuccess { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        [MaxLength(100)]
        public string? EmailType { get; set; } // Overdue, Reminder, Welcome, etc.

        [MaxLength(50)]
        public string? Status { get; set; } // Queued, Sent, Failed
    }
}
