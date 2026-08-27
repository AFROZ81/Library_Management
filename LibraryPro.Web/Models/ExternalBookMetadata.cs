namespace LibraryPro.Web.Models;

public class ExternalBookMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int PublicationYear { get; set; }
    public List<string> Genres { get; set; } = new();
    public string? Publisher { get; set; }
    public int PageCount { get; set; }
    public string Source { get; set; } = string.Empty; // "Google Books" or "Open Library"
}
