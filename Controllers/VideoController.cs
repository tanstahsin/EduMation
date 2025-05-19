using EduMation.Data;
using EduMation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduMation.Controllers
{
    [Authorize]
    public class VideosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VideosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            Console.WriteLine("VideosController instantiated.");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            Console.WriteLine("Index action called.");
            var videos = await _context.Videos.ToListAsync();
            if (!videos.Any())
            {
                Console.WriteLine("No videos found in the database.");
            }
            return View(videos);
        }

        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            Console.WriteLine($"Details action called with id: {id}");
            if (id == null)
            {
                Console.WriteLine("Details action called with null id.");
                return NotFound("Video ID was not provided.");
            }

            var video = await _context.Videos.FirstOrDefaultAsync(m => m.Id == id);
            if (video == null)
            {
                Console.WriteLine($"Video with ID {id} not found in the database.");
                return NotFound($"Video with ID {id} does not exist.");
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"User ID: {userId}");

            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (subscription != null)
            {
                Console.WriteLine($"Subscription found for user {userId}. LastWatchedMonth: {subscription.LastWatchedMonth}, LastWatchedYear: {subscription.LastWatchedYear}");

                // Update watch history
                var watchHistory = await _context.WatchHistories
                    .FirstOrDefaultAsync(wh => wh.UserId == userId && wh.VideoId == video.Id);

                if (watchHistory == null)
                {
                    watchHistory = new WatchHistory
                    {
                        UserId = userId,
                        VideoId = video.Id,
                        WatchCount = 1,
                        WatchDate = DateTime.Now
                    };
                    _context.WatchHistories.Add(watchHistory);
                }
                else
                {
                    watchHistory.WatchCount += 1;
                    watchHistory.WatchDate = DateTime.Now;
                    _context.WatchHistories.Update(watchHistory);
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving watch history: {ex.Message}");
                    TempData["ErrorMessage"] = "Failed to record watch history. Please try again.";
                    return View("Error");
                }

                // Update TotalWatched
                var totalWatched = await _context.WatchHistories
                    .Where(wh => wh.UserId == userId && wh.WatchDate >= subscription.StartDate)
                    .SumAsync(wh => wh.WatchCount);
                subscription.TotalWatched = totalWatched;
                _context.Subscriptions.Update(subscription);

                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Updated TotalWatched for user {userId}: {totalWatched}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating subscription: {ex.Message}");
                    TempData["ErrorMessage"] = "Failed to update subscription data. Please try again.";
                    return View("Error");
                }

                if (subscription.MaxVideos > 0 && subscription.TotalWatched >= subscription.MaxVideos)
                {
                    TempData["ErrorMessage"] = "🚫 You've hit your video limit!<br>Level up your learning—upgrade your subscription to unlock more inspiring content!";
                    return View("Error");
                }

                ViewBag.TotalWatched = totalWatched;
            }
            else
            {
                Console.WriteLine($"No subscription found for user {userId}.");
                ViewBag.TotalWatched = 0;
            }

            ViewBag.IsFavorited = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.VideoId == video.Id);

            return View(video);
        }

        [AllowAnonymous]
        public IActionResult Test()
        {
            return Content("VideosController is reachable.");
        }
    }
}