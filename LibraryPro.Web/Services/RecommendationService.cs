using LibraryPro.Web.Data;
using LibraryPro.Web.Models;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IBookRepository bookRepository,
            ILoanRepository loanRepository,
            IMemberRepository memberRepository,
            ApplicationDbContext context,
            ILogger<RecommendationService> logger)
        {
            _bookRepository = bookRepository;
            _loanRepository = loanRepository;
            _memberRepository = memberRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<List<BookRecommendation>> GetRecommendationsAsync(string? userId = null, int topN = 10)
        {
            var allBooks = await _bookRepository.GetAllAsync();
            var allLoans = await _loanRepository.GetAllLoansAsync();
            var recommendations = new List<BookRecommendation>();

            if (!string.IsNullOrEmpty(userId))
            {
                // Get user's borrowing history
                var member = await _memberRepository.GetByIdAsync(int.Parse(userId));
                if (member != null)
                {
                    var memberLoans = allLoans.Where(l => l.MemberId == member.Id).ToList();
                    var borrowedGenres = memberLoans
                        .SelectMany(l => allBooks.FirstOrDefault(b => b.Id == l.BookId)?.Genre ?? new List<string>())
                        .GroupBy(g => g)
                        .OrderByDescending(g => g.Count())
                        .Take(3)
                        .Select(g => g.Key)
                        .ToList();

                    // Recommend books from favorite genres that user hasn't borrowed
                    var borrowedBookIds = memberLoans.Select(l => l.BookId).ToHashSet();
                    recommendations = allBooks
                        .Where(b => b.AvailableCopies > 0 && !borrowedBookIds.Contains(b.Id))
                        .Where(b => b.Genre.Any(g => borrowedGenres.Contains(g)))
                        .Take(topN)
                        .Select(b => new BookRecommendation
                        {
                            BookId = b.Id,
                            Title = b.Title,
                            Author = b.Author,
                            Genre = b.Genre,
                            ImageUrl = b.ImageUrl,
                            RelevanceScore = 0.8,
                            RecommendationReason = "Based on your reading preferences"
                        })
                        .ToList();
                }
            }

            // If no user-specific recommendations or not enough, add popular books
            if (recommendations.Count < topN)
            {
                var popularBooks = allLoans
                    .GroupBy(l => l.BookId)
                    .OrderByDescending(g => g.Count())
                    .Take(topN - recommendations.Count)
                    .Select(g => new
                    {
                        BookId = g.Key,
                        TimesBorrowed = g.Count()
                    })
                    .ToList();

                foreach (var popular in popularBooks)
                {
                    var book = allBooks.FirstOrDefault(b => b.Id == popular.BookId);
                    if (book != null && !recommendations.Any(r => r.BookId == book.Id))
                    {
                        recommendations.Add(new BookRecommendation
                        {
                            BookId = book.Id,
                            Title = book.Title,
                            Author = book.Author,
                            Genre = book.Genre,
                            ImageUrl = book.ImageUrl,
                            RelevanceScore = 0.7,
                            RecommendationReason = $"Popular book (borrowed {popular.TimesBorrowed} times)"
                        });
                    }
                }
            }

            return recommendations.OrderByDescending(r => r.RelevanceScore).ToList();
        }

        public async Task<List<BookRecommendation>> GetRecommendationsByBookAsync(int bookId, int topN = 5)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return new List<BookRecommendation>();
            }

            var allBooks = await _bookRepository.GetAllAsync();
            var allLoans = await _loanRepository.GetAllLoansAsync();

            // Find books with similar genres
            var similarBooks = allBooks
                .Where(b => b.Id != bookId && b.AvailableCopies > 0)
                .Where(b => b.Genre.Any(g => book.Genre.Contains(g)))
                .Select(b => new
                {
                    Book = b,
                    GenreMatchCount = b.Genre.Count(g => book.Genre.Contains(g))
                })
                .OrderByDescending(b => b.GenreMatchCount)
                .Take(topN)
                .Select(b => new BookRecommendation
                {
                    BookId = b.Book.Id,
                    Title = b.Book.Title,
                    Author = b.Book.Author,
                    Genre = b.Book.Genre,
                    ImageUrl = b.Book.ImageUrl,
                    RelevanceScore = 0.6 + (b.GenreMatchCount * 0.1),
                    RecommendationReason = $"Similar to '{book.Title}' ({b.GenreMatchCount} matching genres)"
                })
                .ToList();

            return similarBooks;
        }

        public async Task<List<BookRecommendation>> GetRecommendationsByGenreAsync(string genre, int topN = 10)
        {
            var allBooks = await _bookRepository.GetAllAsync();
            var allLoans = await _loanRepository.GetAllLoansAsync();

            var genreBooks = allBooks
                .Where(b => b.AvailableCopies > 0 && b.Genre.Contains(genre))
                .OrderByDescending(b => allLoans.Count(l => l.BookId == b.Id))
                .Take(topN)
                .Select(b => new BookRecommendation
                {
                    BookId = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Genre = b.Genre,
                    ImageUrl = b.ImageUrl,
                    RelevanceScore = 0.75,
                    RecommendationReason = $"Popular {genre} book"
                })
                .ToList();

            return genreBooks;
        }

        public async Task<List<Book>> AdvancedSearchAsync(SearchCriteria criteria, int pageNumber = 1, int pageSize = 12)
        {
            var allBooks = await _bookRepository.GetAllAsync();
            var allLoans = await _loanRepository.GetAllLoansAsync();

            var query = allBooks.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.Title))
            {
                query = query.Where(b => b.Title.Contains(criteria.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(criteria.Author))
            {
                query = query.Where(b => b.Author.Contains(criteria.Author, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(criteria.ISBN))
            {
                query = query.Where(b => b.ISBN.Contains(criteria.ISBN, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(criteria.Genre))
            {
                query = query.Where(b => b.Genre.Contains(criteria.Genre, StringComparer.OrdinalIgnoreCase));
            }

            if (criteria.MinPublicationYear.HasValue)
            {
                query = query.Where(b => b.PublicationYear >= criteria.MinPublicationYear.Value);
            }

            if (criteria.MaxPublicationYear.HasValue)
            {
                query = query.Where(b => b.PublicationYear <= criteria.MaxPublicationYear.Value);
            }

            if (criteria.AvailableOnly == true)
            {
                query = query.Where(b => b.AvailableCopies > 0);
            }

            // Apply sorting
            query = criteria.SortBy?.ToLower() switch
            {
                "title" => criteria.SortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(b => b.Title) 
                    : query.OrderBy(b => b.Title),
                "author" => criteria.SortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(b => b.Author) 
                    : query.OrderBy(b => b.Author),
                "publicationyear" => criteria.SortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(b => b.PublicationYear) 
                    : query.OrderBy(b => b.PublicationYear),
                "popularity" => criteria.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(b => allLoans.Count(l => l.BookId == b.Id))
                    : query.OrderBy(b => allLoans.Count(l => l.BookId == b.Id)),
                _ => query.OrderBy(b => b.Title)
            };

            // Pagination
            var totalResults = query.Count();
            var paginatedResults = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogInformation("Advanced search returned {Count} results out of {Total}", 
                paginatedResults.Count, totalResults);

            return paginatedResults;
        }
    }
}
