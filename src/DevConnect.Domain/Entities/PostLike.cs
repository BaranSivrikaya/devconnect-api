namespace DevConnect.Domain.Entities;

public class PostLike : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }

    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}