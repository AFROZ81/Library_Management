using LibraryPro.Web.Models;
using System.Text.Json;

namespace LibraryPro.Web.Services;

public class OpenLibraryService : IExternalBookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenLibraryService> _logger;

    public OpenLibraryService(HttpClient httpClient, ILogger<OpenLibraryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalBookMetadata?> SearchByISBNAsync(string isbn)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://openlibrary.org/api/books?bibkeys=ISBN:{isbn}&format=json&jscmd=data");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, OpenLibraryBook>>(json);

            if (data != null && data.TryGetValue($"ISBN:{isbn}", out var book) && book != null)
            {
                return MapToMetadata(book, "Open Library");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Open Library by ISBN: {ISBN}", isbn);
            return null;
        }
    }

    public async Task<IEnumerable<ExternalBookMetadata>> SearchByTitleAsync(string title, int maxResults = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://openlibrary.org/search.json?q={Uri.EscapeDataString(title)}&limit={maxResults}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<OpenLibrarySearchResponse>(json);

            if (data?.Docs != null)
            {
                return data.Docs.Take(maxResults).Select(doc => new ExternalBookMetadata
                {
                    Title = doc.Title ?? string.Empty,
                    Author = doc.AuthorName != null ? string.Join(", ", doc.AuthorName) : string.Empty,
                    ISBN = doc.Isbn?.FirstOrDefault() ?? string.Empty,
                    Description = doc.Description,
                    ImageUrl = doc.CoverI != null ? $"https://covers.openlibrary.org/b/id/{doc.CoverI}-M.jpg" : null,
                    PublicationYear = doc.FirstPublishYear ?? 0,
                    Genres = doc.Subject ?? new List<string>(),
                    Publisher = doc.Publisher?.FirstOrDefault(),
                    PageCount = doc.NumberOfPages ?? 0,
                    Source = "Open Library"
                });
            }

            return Enumerable.Empty<ExternalBookMetadata>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Open Library by title: {Title}", title);
            return Enumerable.Empty<ExternalBookMetadata>();
        }
    }

    private ExternalBookMetadata? MapToMetadata(OpenLibraryBook book, string source)
    {
        return new ExternalBookMetadata
        {
            Title = book.Title ?? string.Empty,
            Author = book.Authors != null ? string.Join(", ", book.Authors.Select(a => a.Name)) : string.Empty,
            ISBN = book.Isbn_13?.FirstOrDefault() ?? book.Isbn_10?.FirstOrDefault() ?? string.Empty,
            Description = book.Notes,
            ImageUrl = book.Cover != null ? $"https://covers.openlibrary.org/b/id/{book.Cover}-M.jpg" : null,
            PublicationYear = book.PublishDate != null ? 
                int.TryParse(book.PublishDate.Substring(0, 4), out var year) ? year : 0 : 0,
            Genres = book.Subjects?.Select(s => s.Name).ToList() ?? new List<string>(),
            Publisher = book.Publishers?.FirstOrDefault(),
            PageCount = book.NumberOfPages ?? 0,
            Source = source
        };
    }

    private class OpenLibraryBook
    {
        public string? Title { get; set; }
        public List<OpenLibraryAuthor>? Authors { get; set; }
        public List<string>? Isbn_10 { get; set; }
        public List<string>? Isbn_13 { get; set; }
        public string? PublishDate { get; set; }
        public string? Notes { get; set; }
        public int? Cover { get; set; }
        public List<OpenLibrarySubject>? Subjects { get; set; }
        public List<string>? Publishers { get; set; }
        public int? NumberOfPages { get; set; }
    }

    private class OpenLibraryAuthor
    {
        public string? Name { get; set; }
    }

    private class OpenLibrarySubject
    {
        public string? Name { get; set; }
    }

    private class OpenLibrarySearchResponse
    {
        public List<OpenLibraryDoc>? Docs { get; set; }
    }

    private class OpenLibraryDoc
    {
        public string? Title { get; set; }
        public List<string>? AuthorName { get; set; }
        public List<string>? Isbn { get; set; }
        public string? Description { get; set; }
        public int? CoverI { get; set; }
        public int? FirstPublishYear { get; set; }
        public List<string>? Subject { get; set; }
        public List<string>? Publisher { get; set; }
        public int? NumberOfPages { get; set; }
    }
}
