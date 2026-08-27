using LibraryPro.Web.Models;
using System.Text.Json;

namespace LibraryPro.Web.Services;

public class GoogleBooksService : IExternalBookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleBooksService> _logger;

    public GoogleBooksService(HttpClient httpClient, ILogger<GoogleBooksService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalBookMetadata?> SearchByISBNAsync(string isbn)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GoogleBooksResponse>(json);

            if (data?.Items != null && data.Items.Count > 0)
            {
                return MapToMetadata(data.Items[0], "Google Books");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Google Books by ISBN: {ISBN}", isbn);
            return null;
        }
    }

    public async Task<IEnumerable<ExternalBookMetadata>> SearchByTitleAsync(string title, int maxResults = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://www.googleapis.com/books/v1/volumes?q=intitle:{title}&maxResults={maxResults}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GoogleBooksResponse>(json);

            if (data?.Items != null)
            {
                return data.Items.Select(item => MapToMetadata(item, "Google Books")).Where(m => m != null).OfType<ExternalBookMetadata>();
            }

            return Enumerable.Empty<ExternalBookMetadata>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Google Books by title: {Title}", title);
            return Enumerable.Empty<ExternalBookMetadata>();
        }
    }

    private ExternalBookMetadata? MapToMetadata(GoogleBookItem item, string source)
    {
        if (item.VolumeInfo == null) return null;

        return new ExternalBookMetadata
        {
            Title = item.VolumeInfo.Title ?? string.Empty,
            Author = item.VolumeInfo.Authors != null ? string.Join(", ", item.VolumeInfo.Authors) : string.Empty,
            ISBN = item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier ?? 
                   item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier ?? string.Empty,
            Description = item.VolumeInfo.Description,
            ImageUrl = item.VolumeInfo.ImageLinks?.Thumbnail ?? item.VolumeInfo.ImageLinks?.SmallThumbnail,
            PublicationYear = item.VolumeInfo.PublishedDate != null ? 
                int.TryParse(item.VolumeInfo.PublishedDate.Substring(0, 4), out var year) ? year : 0 : 0,
            Genres = item.VolumeInfo.Categories ?? new List<string>(),
            Publisher = item.VolumeInfo.Publisher,
            PageCount = item.VolumeInfo.PageCount,
            Source = source
        };
    }

    private class GoogleBooksResponse
    {
        public List<GoogleBookItem>? Items { get; set; }
    }

    private class GoogleBookItem
    {
        public GoogleVolumeInfo? VolumeInfo { get; set; }
    }

    private class GoogleVolumeInfo
    {
        public string? Title { get; set; }
        public List<string>? Authors { get; set; }
        public string? PublishedDate { get; set; }
        public string? Description { get; set; }
        public List<string>? Categories { get; set; }
        public string? Publisher { get; set; }
        public int PageCount { get; set; }
        public GoogleImageLinks? ImageLinks { get; set; }
        public List<GoogleIndustryIdentifier>? IndustryIdentifiers { get; set; }
    }

    private class GoogleImageLinks
    {
        public string? SmallThumbnail { get; set; }
        public string? Thumbnail { get; set; }
    }

    private class GoogleIndustryIdentifier
    {
        public string? Type { get; set; }
        public string? Identifier { get; set; }
    }
}
