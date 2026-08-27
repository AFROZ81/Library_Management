namespace LibraryPro.Web.Models.Api;

public class ApiBookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int TotalCopies { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int TotalCopies { get; set; }
    public string? ImageUrl { get; set; }
}
