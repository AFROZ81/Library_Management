using LibraryPro.Web.Models.ViewModels;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LibraryPro.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBookRepository _bookRepo;
        private readonly IMemberRepository _memberRepo;
        private readonly ILoanRepository _loanRepo;

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
            // 1. Fetch data from repositories
            var books = (await _bookRepo.GetAllAsync()).ToList();
            var members = (await _memberRepo.GetAllAsync()).ToList();
            var allLoans = (await _loanRepo.GetAllLoansAsync()).ToList();

            var today = DateTime.Now.Date;

            // 2. Chart Logic: Last 7 Days
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var weeklyLabels = last7Days.Select(d => d.ToString("MMM dd")).ToList();
            var weeklyCounts = last7Days.Select(day =>
                allLoans.Count(l => l.LoanDate.Date == day.Date)).ToList();

            // 3. Critical Alerts
            var criticalOverdue = allLoans
                .Where(l => !l.IsReturned && l.DueDate.Date < today)
                .OrderByDescending(l => (today - l.DueDate.Date).TotalDays)
                .Take(5)
                .ToList();

            // 4. Core Metrics
            int activeLoansCount = allLoans.Count(l => !l.IsReturned);
            decimal totalFines = allLoans
                .Where(l => !l.IsReturned && l.DueDate.Date < today)
                .Sum(l => (decimal)(today - l.DueDate.Date).TotalDays * 10);

            // 5. Calculate Top Borrowers
            var topBorrowersList = allLoans
                .Where(l => l.Member != null)
                .GroupBy(l => l.MemberId)
                .Select(g => new BorrowerStatsViewModel
                {
                    MemberName = g.First().Member.Name,
                    MemberEmail = g.First().Member.Email,
                    LoanCount = g.Count()
                })
                .OrderByDescending(m => m.LoanCount)
                .Take(5)
                .ToList();

            // 6. REAL GENRE DATA LOGIC (Flattening List<string>)
            var genreData = books
                .Where(b => b.Genre != null && b.Genre.Any()) // Ensure the list exists and isn't empty
                .SelectMany(b => b.Genre)                    // Flatten: turns List<List<string>> into List<string>
                .GroupBy(g => g)                             // Group by the genre name
                .Select(g => new {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToList();

            // 7. Assemble and RETURN the ViewModel
            var viewModel = new DashboardViewModel
            {
                TotalBooks = books.Count,
                TotalMembers = members.Count,
                ActiveLoans = activeLoansCount,
                TotalPendingFines = totalFines,

                AvailabilityRate = books.Any() ?
                    ((double)(books.Count - activeLoansCount) / books.Count) * 100 : 0,

                WeeklyLabels = weeklyLabels,
                WeeklyIssueCounts = weeklyCounts,
                CriticalOverdueLoans = criticalOverdue,
                TopBorrowers = topBorrowersList,

                // Assign Real Genre Data
                GenreLabels = genreData.Select(g => g.Name).ToList(),
                GenreCounts = genreData.Select(g => g.Count).ToList()
            };

            return View(viewModel);
        }
    }
}