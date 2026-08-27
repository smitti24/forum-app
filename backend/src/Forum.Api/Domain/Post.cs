namespace Forum.Api.Domain;

public class Post
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }
    public Member Author { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public bool IsFlagged { get; set; }
    public Guid? FlaggedById { get; set; }
    public Member? FlaggedBy { get; set; }
    public DateTime? FlaggedAt { get; set; }
    public string? FlagNote { get; set; }

    public int LikeCount { get; set; }
    public int CommentCount { get; set; }

    public List<Comment> Comments { get; set; } = [];
    public List<Like> Likes { get; set; } = [];
}
