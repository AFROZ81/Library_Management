using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Models.Entities
{
    public enum ReservationStatus
    {
        Pending,
        Fulfilled,
        Cancelled,
        Expired
    }

    public class BookReservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book? Book { get; set; }

        [Required]
        public int MemberId { get; set; }
        public Member? Member { get; set; }

        public DateTime ReservationDate { get; set; } = DateTime.Now;

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public int QueuePosition { get; set; } = 1;

        public DateTime? ExpirationDate { get; set; }
    }
}
