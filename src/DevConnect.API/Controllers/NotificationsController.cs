using System.Security.Claims;
using DevConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserIdText) || !Guid.TryParse(currentUserIdText, out var currentUserId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        var notifications = await _context.Notifications
            .Where(x => x.UserId == currentUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Message,
                x.IsRead,
                x.CreatedAt,
                x.UserId
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [Authorize]
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserIdText) || !Guid.TryParse(currentUserIdText, out var currentUserId))
        {
            return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == currentUserId);

        if (notification == null)
        {
            return NotFound(new { message = "Bildirim bulunamadı." });
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Bildirim okundu olarak işaretlendi." });
    }
}