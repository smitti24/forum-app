namespace Forum.Api.Domain;

public class Comment
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public Member Author { get; set; } = null!;

    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
