using System.Security.Claims;
using DevConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedController : ControllerBase
{
    private readonly AppDbContext _context;

    public FeedController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMyFeed()
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserIdText) || !Guid.TryParse(currentUserIdText, out var currentUserId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        var followingIds = await _context.Follows
            .Where(x => x.FollowerId == currentUserId)
            .Select(x => x.FollowingId)
            .ToListAsync();

        var posts = await _context.Posts
            .Where(x => followingIds.Contains(x.UserId))
            .Include(x => x.User)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Content,
                x.ImageUrl,
                x.CreatedAt,
                likeCount = x.Likes.Count,
                commentCount = x.Comments.Count,
                User = new
                {
                    x.User.Id,
                    x.User.FullName,
                    x.User.UserName,
                    x.User.ProfileImageUrl
                }
            })
            .ToListAsync();

        return Ok(posts);
    }
}