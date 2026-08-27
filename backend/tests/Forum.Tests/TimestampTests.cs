using Forum.Api.Domain;

namespace Forum.Tests;

public class TimestampTests
{
    [Fact]
    public async Task A_timestamp_round_trips_through_sqlite()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        var written = new DateTime(2026, 3, 14, 15, 9, 26, DateTimeKind.Utc).AddTicks(5358979);

        await factory.WithDbAsync(async db =>
        {
            db.Members.Add(NewMember("asmith", written));
            await db.SaveChangesAsync();
        });

        var read = await factory.WithDbAsync(db => db.Members.Select(m => m.CreatedAt).SingleAsync());

        Assert.Equal(written, read);
    }

    [Fact]
    public async Task Posts_are_ordered_by_creation_time_in_sql()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        await SeedThreePostsAsync(factory);

        var query = await factory.WithDbAsync(db => Task.FromResult(
            db.Posts.OrderByDescending(p => p.CreatedAt).Select(p => p.Title).ToQueryString()));

        var titles = await factory.WithDbAsync(db =>
            db.Posts.OrderByDescending(p => p.CreatedAt).Select(p => p.Title).ToListAsync());

        Assert.Contains("ORDER BY", query);
        Assert.Equal(["newest", "middle", "oldest"], titles);
    }

    [Fact]
    public async Task Posts_are_filtered_by_a_date_range_in_sql()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        await SeedThreePostsAsync(factory);

        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        var query = await factory.WithDbAsync(db => Task.FromResult(
            db.Posts.Where(p => p.CreatedAt >= from && p.CreatedAt <= to).Select(p => p.Title).ToQueryString()));

        var titles = await factory.WithDbAsync(db =>
            db.Posts.Where(p => p.CreatedAt >= from && p.CreatedAt <= to).Select(p => p.Title).ToListAsync());

        Assert.Contains("WHERE", query);
        Assert.Equal(["middle"], titles);
    }

    [Fact]
    public async Task Posts_are_ordered_by_like_count_in_sql()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        await SeedThreePostsAsync(factory);

        var titles = await factory.WithDbAsync(db =>
            db.Posts.OrderByDescending(p => p.LikeCount).Select(p => p.Title).ToListAsync());

        Assert.Equal(["middle", "oldest", "newest"], titles);
    }

    private static async Task SeedThreePostsAsync(ForumApiFactory factory) =>
        await factory.WithDbAsync(async db =>
        {
            var author = NewMember("asmith", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            db.Members.Add(author);

            db.Posts.AddRange(
                NewPost(author.Id, "oldest", new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), likes: 5),
                NewPost(author.Id, "middle", new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), likes: 9),
                NewPost(author.Id, "newest", new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), likes: 1));

            await db.SaveChangesAsync();
        });

    private static Member NewMember(string username, DateTime createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = $"{username}@example.com",
        EmailNormalized = $"{username}@example.com",
        Username = username,
        UsernameNormalized = username,
        PasswordHash = "hash",
        Role = MemberRole.Member,
        CreatedAt = createdAt
    };

    private static Post NewPost(Guid authorId, string title, DateTime createdAt, int likes) => new()
    {
        Id = Guid.CreateVersion7(),
        AuthorId = authorId,
        Title = title,
        Body = "body",
        CreatedAt = createdAt,
        LikeCount = likes
    };
}
