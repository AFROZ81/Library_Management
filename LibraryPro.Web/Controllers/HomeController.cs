using LibraryPro.Web.Models.ViewModels;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

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
            // 1. Fetch data from repos
            var books = (await _bookRepo.GetAllAsync()).ToList();
            var members = (await _memberRepo.GetAllAsync()).ToList();
            var allLoans = (await _loanRepo.GetAllLoansAsync()).ToList();

            var today = DateTime.Now.Date;

            // 2. Chart Logic: Generate exactly 7 days of data (including 0s)
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var weeklyLabels = last7Days.Select(d => d.ToString("MMM dd")).ToList();
            var weeklyCounts = last7Days.Select(day =>
                allLoans.Count(l => l.LoanDate.Date == day.Date)).ToList();

            // 3. Critical Alerts: Not returned AND > 3 days overdue
            var criticalOverdue = allLoans
                .Where(l => !l.IsReturned && (today - l.DueDate.Date).TotalDays > 3)
                .OrderByDescending(l => (today - l.DueDate.Date).TotalDays)
                .Take(5)
                .ToList();

            // 4. Core Metrics Calculations
            int activeLoansCount = allLoans.Count(l => !l.IsReturned);
            decimal totalFines = allLoans
                .Where(l => !l.IsReturned && l.DueDate < today)
                .Sum(l => (decimal)(today - l.DueDate.Date).TotalDays * 10); // 10 per day fine

            // 5. Assemble ViewModel
            var viewModel = new DashboardViewModel
            {
                TotalBooks = books.Count,
                TotalMembers = members.Count,
                ActiveLoans = activeLoansCount,
                TotalPendingFines = totalFines,

                // Inventory Health: (Available / Total) * 100
                AvailabilityRate = books.Any() ?
                    ((double)(books.Count - activeLoansCount) / books.Count) * 100 : 0,

                WeeklyLabels = weeklyLabels,
                WeeklyIssueCounts = weeklyCounts,
                CriticalOverdueLoans = criticalOverdue,

                // Dummy data for the Genre Chart (Recruiters love visual placeholders)
                GenreLabels = new List<string> { "Fiction", "Tech", "Science", "History" },
                GenreCounts = new List<int> { 40, 25, 15, 20 }
            };

            var topMembers = allLoans
            .Where(l => l.Member != null)
            .GroupBy(l => l.MemberId)
            .Select(g => new TopBorrowerDTO
            {
                MemberName = g.First().Member.Name,
                MemberEmail = g.First().Member.Email, // Use Email since Status doesn't exist
                LoanCount = g.Count()
            })
            .OrderByDescending(m => m.LoanCount)
            .Take(5)
            .ToList();

            return View(viewModel);
        }
    }
}