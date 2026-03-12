using System.Security.Claims;
using DevConnect.Application.DTO;
using DevConnect.Domain.Entities;
using DevConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PostsController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost(PostCreateRequestDto request)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdText) || !Guid.TryParse(userIdText, out var userId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == userId);
        if (!userExists)
        {
            return Unauthorized(new { message = "Kullanıcı bulunamadı." });
        }

        var post = new Post
        {
            UserId = userId,
            Content = request.Content,
            ImageUrl = request.ImageUrl
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Post başarıyla oluşturuldu.",
            post.Id,
            post.Content,
            post.ImageUrl,
            post.UserId,
            post.CreatedAt
        });
    }

   [Authorize]
[HttpPost("{postId:guid}/like")]
public async Task<IActionResult> LikePost(Guid postId)
{
    var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(currentUserIdText) || !Guid.TryParse(currentUserIdText, out var currentUserId))
    {
        return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
    }

    var postExists = await _context.Posts.AnyAsync(x => x.Id == postId);
    if (!postExists)
    {
        return NotFound(new { message = "Post bulunamadı." });
    }

    var alreadyLiked = await _context.PostLikes
        .AnyAsync(x => x.PostId == postId && x.UserId == currentUserId);

    if (alreadyLiked)
    {
        return BadRequest(new { message = "Bu post zaten beğenildi." });
    }

    var like = new PostLike
    {
        PostId = postId,
        UserId = currentUserId
    };

    _context.PostLikes.Add(like);

    var post = await _context.Posts
        .Include(x => x.User)
        .FirstOrDefaultAsync(x => x.Id == postId);

    var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);

    if (post != null && currentUser != null && post.UserId != currentUserId)
    {
        var notification = new Domain.Entities.Notification
        {
            UserId = post.UserId,
            Message = $"{currentUser.UserName} postunu beğendi."
        };

        _context.Notifications.Add(notification);
    }

    await _context.SaveChangesAsync();

    return Ok(new { message = "Post beğenildi." });
}

    [Authorize]
    [HttpDelete("{postId:guid}/like")]
    public async Task<IActionResult> UnlikePost(Guid postId)
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdText) || !Guid.TryParse(userIdText, out var userId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        var like = await _context.PostLikes
            .FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);

        if (like == null)
        {
            return NotFound(new { message = "Beğeni kaydı bulunamadı." });
        }

        _context.PostLikes.Remove(like);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Beğeni kaldırıldı." });
    }
[HttpGet]
public async Task<IActionResult> GetAllPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
{
    if (page <= 0) page = 1;
    if (pageSize <= 0) pageSize = 10;
    if (pageSize > 50) pageSize = 50;

    var totalCount = await _context.Posts.CountAsync();

    var posts = await _context.Posts
        .Include(x => x.User)
        .Include(x => x.Likes)
        .OrderByDescending(x => x.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new
        {
            x.Id,
            x.Content,
            x.ImageUrl,
            x.CreatedAt,
            likeCount = x.Likes.Count,
            User = new
            {
                x.User.Id,
                x.User.FullName,
                x.User.UserName
            }
        })
        .ToListAsync();

    return Ok(new
    {
        page,
        pageSize,
        totalCount,
        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        items = posts
    });
}
    
}