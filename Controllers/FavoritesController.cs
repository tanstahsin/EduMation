using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduMation.Data;
using EduMation.Models;
using System.Security.Claims;

namespace EduMation.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Add(int videoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.VideoId == videoId);

            if (favorite == null)
            {
                favorite = new Favorites { UserId = userId, VideoId = videoId };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyFavorites", "Favorites");
        }

        public async Task<IActionResult> MyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var favorites = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Video)
                    .Select(f => f.Video)
                    .ToListAsync();

                // Filter out null videos (in case a video was deleted)
                favorites = favorites.Where(v => v != null).ToList();

                if (favorites == null)
                {
                    return View(new List<Video>());
                }

                return View(favorites);
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using ILogger or console for debugging)
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An error occurred while retrieving your favorites.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int videoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.VideoId == videoId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyFavorites", "Favorites");
        }
    }
}