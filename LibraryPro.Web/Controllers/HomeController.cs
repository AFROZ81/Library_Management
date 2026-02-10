using LibraryPro.Web.Models.ViewModels;
using LibraryPro.Web.Repositories; // Make sure this matches your repo namespace
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers
{
    public class HomeController : Controller
    {
        // 1. Define the private fields for your repositories
        private readonly IBookRepository _bookRepo;
        private readonly IMemberRepository _memberRepo;
        private readonly ILoanRepository _loanRepo;

        // 2. Inject them through the constructor
        public HomeController(
            IBookRepository bookRepo,
            IMemberRepository memberRepo,
            ILoanRepository loanRepo)
        {
            _bookRepo = bookRepo;
            _memberRepo = memberRepo;
            _loanRepo = loanRepo;
        }

        public async Task<IActionResult> Index()
        {
            var books = await _bookRepo.GetAllAsync();
            var loans = await _loanRepo.GetAllLoansAsync();
            var members = await _memberRepo.GetAllAsync();

            // Calculate total physical inventory
            int totalPhysicalStock = books.Sum(b => b.TotalCopies);
            int currentAvailable = books.Sum(b => b.AvailableCopies);

            var model = new DashboardViewModel
            {
                // Reverting this to Sum to show "55"
                TotalBooks = totalPhysicalStock,

                TotalMembers = members.Count(),
                ActiveLoans = loans.Count(l => !l.IsReturned),
                TotalPendingFines = loans.Where(l => !l.IsReturned).Sum(l => l.CalculateLateFee),

                // Keeping the availability rate for the progress bar
                AvailabilityRate = totalPhysicalStock > 0 ? (double)currentAvailable / totalPhysicalStock * 100 : 0,

                GenreLabels = books.SelectMany(b => b.Genre).Distinct().Take(5).ToList(),
                GenreCounts = books.SelectMany(b => b.Genre).GroupBy(g => g).Select(g => g.Count()).Take(5).ToList()
            };

            return View(model);
        }
    }
}