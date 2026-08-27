namespace Forum.Api.Domain;

public class Like
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
