using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Models.Entities
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Title { get; set; }

        [Required]
        public string? Author { get; set; }

        public string? ISBN { get; set; }

        [Range(1000, 2100, ErrorMessage = "Please enter a valid year")]
        [Display(Name = "Publication Year")]
        public int PublicationYear { get; set; } // Changed from PublishedDate
        public List<string> Genre { get; set; } = new List<string>();

        [Range(0, 1000)]
        public int TotalCopies { get; set; }

        public int AvailableCopies { get; set; }

        public string? ImageUrl { get; set; } // For the "Eye-catching" UI
    }
}