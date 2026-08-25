using LibraryPro.Web.Models;
using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Services
{
    public interface IRecommendationService
    {
        Task<List<BookRecommendation>> GetRecommendationsAsync(string? userId = null, int topN = 10);
        Task<List<BookRecommendation>> GetRecommendationsByBookAsync(int bookId, int topN = 5);
        Task<List<BookRecommendation>> GetRecommendationsByGenreAsync(string genre, int topN = 10);
        Task<List<Book>> AdvancedSearchAsync(SearchCriteria criteria, int pageNumber = 1, int pageSize = 12);
    }
}
