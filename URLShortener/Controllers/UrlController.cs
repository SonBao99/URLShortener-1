using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using URLShortener.Models;

[Route("")]
[ApiController]
public class UrlController : ControllerBase
{
    private readonly UrlContext _context;
    private readonly Regex _aliasRegex = new Regex("^[a-zA-Z0-9-_]+$");

    public UrlController(UrlContext context)
    {
        _context = context;
    }

    // POST api/url/shorten
    [HttpPost("api/url/shorten")]
    public async Task<IActionResult> ShortenUrl([FromBody] UrlRequest request)
    {
        if (!Uri.IsWellFormedUriString(request.OriginalUrl, UriKind.Absolute))
            return BadRequest(new { message = "Invalid URL format." });

        // Validate and process custom alias
        string shortCode;
        if (!string.IsNullOrEmpty(request.CustomAlias))
        {
            if (!_aliasRegex.IsMatch(request.CustomAlias))
                return BadRequest(new { message = "Custom alias can only contain letters, numbers, hyphens, and underscores." });

            if (await _context.Urls.AnyAsync(u => u.ShortenedCode == request.CustomAlias))
                return BadRequest(new { message = "This custom alias is already in use." });

            shortCode = request.CustomAlias;
        }
        else
        {
            shortCode = await GenerateUniqueShortCodeAsync();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Process expiration date
        DateTime? expirationDate = null;
        if (request.Expiration.HasValue)
        {
            if (request.Expiration.Value <= DateTime.UtcNow)
                return BadRequest(new { message = "Expiration date must be in the future." });

            expirationDate = request.Expiration.Value;
        }

        var url = new Url
        {
            OriginalUrl = request.OriginalUrl,
            ShortenedCode = shortCode,
            CreatedAt = DateTime.UtcNow,
            ExpirationDate = expirationDate,
            UserId = userId,
            HitCount = 0
        };

        try
        {
            _context.Urls.Add(url);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                shortenedUrl = $"{Request.Scheme}://{Request.Host}/{shortCode}",
                expiresAt = url.ExpirationDate
            });
        }
        catch (DbUpdateException)
        {
            // Handle potential race condition with custom aliases
            if (!string.IsNullOrEmpty(request.CustomAlias))
                return BadRequest(new { message = "This custom alias is already in use." });

            // For auto-generated codes, try again
            return await ShortenUrl(request);
        }
    }

    // GET /{shortCode} - catch-all route for redirection
    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectToOriginalUrl(string shortCode)
    {
        if (string.IsNullOrEmpty(shortCode))
            return BadRequest(new { message = "Short code cannot be empty." });

        var url = await _context.Urls.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ShortenedCode == shortCode);

        if (url == null)
            return NotFound(new { message = "URL not found." });

        // Check if the URL has expired
        if (url.ExpirationDate.HasValue && url.ExpirationDate.Value < DateTime.UtcNow)
            return BadRequest(new { message = "This URL has expired." });

        // Update hit count asynchronously - don't wait for it to complete
        _ = UpdateHitCount(shortCode);

        return Redirect(url.OriginalUrl);
    }

    private async Task<string> GenerateUniqueShortCodeAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        const int codeLength = 6;
        var random = new Random();
        string shortCode;

        do
        {
            shortCode = new string(Enumerable.Repeat(chars, codeLength)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
        while (await _context.Urls.AnyAsync(u => u.ShortenedCode == shortCode));

        return shortCode;
    }

    private async Task UpdateHitCount(string shortCode)
    {
        try
        {
            await _context.Urls
                .Where(u => u.ShortenedCode == shortCode)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.HitCount, u => u.HitCount + 1));
        }
        catch
        {
            // Log the error but don't fail the request
        }
    }
}