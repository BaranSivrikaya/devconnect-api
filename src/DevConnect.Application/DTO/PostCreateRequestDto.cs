namespace DevConnect.Application.DTO;

public class PostCreateRequestDto
{
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}