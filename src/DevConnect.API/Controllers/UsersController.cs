using System.Security.Claims;
using DevConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var user = await _context.Users
            .Include(x => x.Posts)
                .ThenInclude(x => x.Likes)
            .Include(x => x.Posts)
                .ThenInclude(x => x.Comments)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        var result = new
        {
            user.Id,
            user.FullName,
            user.UserName,
            user.Email,
            user.Bio,
            user.ProfileImageUrl,
            user.GithubUrl,
            user.LinkedinUrl,
            followerCount = user.Followers.Count,
            followingCount = user.Following.Count,
            totalPostCount = user.Posts.Count,
            posts = user.Posts
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Content,
                    x.ImageUrl,
                    x.CreatedAt,
                    likeCount = x.Likes.Count,
                    commentCount = x.Comments.Count
                })
                .ToList()
        };

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdText) || !Guid.TryParse(userIdText, out var userId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        var user = await _context.Users
            .Include(x => x.Posts)
                .ThenInclude(x => x.Likes)
            .Include(x => x.Posts)
                .ThenInclude(x => x.Comments)
            .Include(x => x.Followers)
            .Include(x => x.Following)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        var result = new
        {
            user.Id,
            user.FullName,
            user.UserName,
            user.Email,
            user.Bio,
            user.ProfileImageUrl,
            user.GithubUrl,
            user.LinkedinUrl,
            followerCount = user.Followers.Count,
            followingCount = user.Following.Count,
            totalPostCount = user.Posts.Count,
            posts = user.Posts
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Content,
                    x.ImageUrl,
                    x.CreatedAt,
                    likeCount = x.Likes.Count,
                    commentCount = x.Comments.Count
                })
                .ToList()
        };

        return Ok(result);
    }

    [Authorize]
    [HttpPost("{userId:guid}/follow")]
    public async Task<IActionResult> FollowUser(Guid userId)
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserIdText) || !Guid.TryParse(currentUserIdText, out var currentUserId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        if (currentUserId == userId)
        {
            return BadRequest(new { message = "Kullanıcı kendisini takip edemez." });
        }

        var targetUserExists = await _context.Users.AnyAsync(x => x.Id == userId);
        if (!targetUserExists)
        {
            return NotFound(new { message = "Takip edilecek kullanıcı bulunamadı." });
        }

        var alreadyFollowing = await _context.Follows
            .AnyAsync(x => x.FollowerId == currentUserId && x.FollowingId == userId);

        if (alreadyFollowing)
        {
            return BadRequest(new { message = "Bu kullanıcı zaten takip ediliyor." });
        }

        var follow = new Domain.Entities.Follow
        {
            FollowerId = currentUserId,
            FollowingId = userId
        };

        _context.Follows.Add(follow);

var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);

if (currentUser != null)
{
    var notification = new Domain.Entities.Notification
    {
        UserId = userId,
        Message = $"{currentUser.UserName} seni takip etti."
    };

    _context.Notifications.Add(notification);
}

await _context.SaveChangesAsync();

return Ok(new { message = "Kullanıcı takip edildi." });

    }

    [Authorize]
    [HttpDelete("{userId:guid}/follow")]
    public async Task<IActionResult> UnfollowUser(Guid userId)
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserIdText) || !Guid.TryParse(currentUserIdText, out var currentUserId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        var follow = await _context.Follows
            .FirstOrDefaultAsync(x => x.FollowerId == currentUserId && x.FollowingId == userId);

        if (follow == null)
        {
            return NotFound(new { message = "Takip kaydı bulunamadı." });
        }

        _context.Follows.Remove(follow);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Takip bırakıldı." });
    }

    [HttpGet("{userId:guid}/followers")]
    public async Task<IActionResult> GetFollowers(Guid userId)
    {
        var userExists = await _context.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        var followers = await _context.Follows
            .Where(x => x.FollowingId == userId)
            .Include(x => x.Follower)
            .Select(x => new
            {
                x.Follower.Id,
                x.Follower.FullName,
                x.Follower.UserName,
                x.Follower.ProfileImageUrl
            })
            .ToListAsync();

        return Ok(followers);
    }

    [HttpGet("{userId:guid}/following")]
    public async Task<IActionResult> GetFollowing(Guid userId)
    {
        var userExists = await _context.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        var following = await _context.Follows
            .Where(x => x.FollowerId == userId)
            .Include(x => x.Following)
            .Select(x => new
            {
                x.Following.Id,
                x.Following.FullName,
                x.Following.UserName,
                x.Following.ProfileImageUrl
            })
            .ToListAsync();

        return Ok(following);
    }
        [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Arama metni boş olamaz." });
        }

        var users = await _context.Users
            .Where(x =>
                x.FullName.ToLower().Contains(query.ToLower()) ||
                x.UserName.ToLower().Contains(query.ToLower()))
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.UserName,
                x.Email,
                x.ProfileImageUrl
            })
            .ToListAsync();

        return Ok(users);
    }

}