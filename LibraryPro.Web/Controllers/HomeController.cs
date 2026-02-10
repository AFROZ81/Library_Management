using LibraryPro.Web.Data;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.TotalBooks = _context.Books.Count();
        ViewBag.TotalMembers = _context.Members.Count();
        ViewBag.ActiveLoans = _context.BookLoans.Count(l => !l.IsReturned);
        ViewBag.OverdueLoans = _context.BookLoans.Count(l => !l.IsReturned && l.DueDate < DateTime.Now);

        return View();
    }
}