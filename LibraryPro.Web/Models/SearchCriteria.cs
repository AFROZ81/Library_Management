using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Models
{
    public class SearchCriteria
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? ISBN { get; set; }
        public string? Genre { get; set; }
        public int? MinPublicationYear { get; set; }
        public int? MaxPublicationYear { get; set; }
        public bool? AvailableOnly { get; set; }
        public string? SortBy { get; set; } // Title, Author, PublicationYear, Popularity, Rating
        public string? SortOrder { get; set; } // Asc, Desc
    }

    public class BookRecommendation
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public List<string> Genre { get; set; } = new();
        public string? ImageUrl { get; set; }
        public double RelevanceScore { get; set; }
        public string RecommendationReason { get; set; } = string.Empty;
    }

    public class AdvancedSearchViewModel
    {
        public SearchCriteria SearchCriteria { get; set; } = new();
        public List<BookRecommendation> Recommendations { get; set; } = new();
        public int TotalResults { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
