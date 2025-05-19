using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduMation.Data;
using EduMation.Models;

namespace EduMation.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string query)
        {
            var videos = string.IsNullOrEmpty(query) ?
                _context.Videos.ToList() :
                _context.Videos
                    .Where(v => EF.Functions.Like(v.Title, $"%{query}%") || EF.Functions.Like(v.Genre, $"%{query}%"))
                    .ToList();

            return View(videos);
        }
    }
}