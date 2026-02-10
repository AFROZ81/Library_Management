using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers
{
    public class MembersController : Controller
    {
        private readonly IMemberRepository _memberRepo;

        public MembersController(IMemberRepository memberRepo) => _memberRepo = memberRepo;

        public async Task<IActionResult> Index()
        {
            var members = await _memberRepo.GetAllAsync();
            return View(members);
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(Member member)
        {
            if (ModelState.IsValid)
            {
                await _memberRepo.AddAsync(member);
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }
    }
}