using System.Security.Claims;
using DevConnect.Application.DTO;
using DevConnect.Domain.Entities;
using DevConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevConnect.API.Controllers;

[ApiController]
[Route("api/Posts/{postId:guid}/comments")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CommentsController(AppDbContext context)
    {
        _context = context;
    }
[Authorize]
[HttpPost("/api/Posts/{postId:guid}/comments")]
public async Task<IActionResult> CreateComment(Guid postId, CommentCreateRequestDto request)
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

    var comment = new Comment
    {
        Content = request.Content,
        PostId = postId,
        UserId = currentUserId
    };

    _context.Comments.Add(comment);

    var post = await _context.Posts
        .Include(x => x.User)
        .FirstOrDefaultAsync(x => x.Id == postId);

    var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == currentUserId);

    if (post != null && currentUser != null && post.UserId != currentUserId)
    {
        var notification = new Domain.Entities.Notification
        {
            UserId = post.UserId,
            Message = $"{currentUser.UserName} postuna yorum yaptı."
        };

        _context.Notifications.Add(notification);
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Yorum başarıyla eklendi.",
        comment.Id,
        comment.Content,
        comment.PostId,
        comment.UserId,
        comment.CreatedAt
    });
}
    

    [HttpGet]
    public async Task<IActionResult> GetCommentsByPost(Guid postId)
    {
        var postExists = await _context.Posts.AnyAsync(x => x.Id == postId);
        if (!postExists)
        {
            return NotFound(new { message = "Post bulunamadı." });
        }

        var comments = await _context.Comments
            .Where(x => x.PostId == postId)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Content,
                x.CreatedAt,
                User = new
                {
                    x.User.Id,
                    x.User.FullName,
                    x.User.UserName
                }
            })
            .ToListAsync();

        return Ok(comments);
    }
}