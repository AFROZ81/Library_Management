using LibraryPro.Web.Models;

namespace LibraryPro.Web.Services;

public interface IExternalBookService
{
    Task<ExternalBookMetadata?> SearchByISBNAsync(string isbn);
    Task<IEnumerable<ExternalBookMetadata>> SearchByTitleAsync(string title, int maxResults = 5);
}
