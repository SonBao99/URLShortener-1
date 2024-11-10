using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using URLShortener.Models;

namespace URLShortener.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UrlContext _urlContext;  // Change this to match the constructor parameter

        public HomeController(ILogger<HomeController> logger, UrlContext urlContext)
        {
            _logger = logger;
            _urlContext = urlContext;  // Ensure you're using _urlContext here
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("myurls")]
        public async Task<IActionResult> MyUrls()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if not authenticated
            }

            // Get the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch all URLs associated with the logged-in user
            var userUrls = await _urlContext.Urls
                .Where(u => u.UserId == userId)
                .ToListAsync();
            // Pass the user ID along with the list of URLs to the view
            ViewBag.UserId = userId;
            return View(userUrls);
            
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
