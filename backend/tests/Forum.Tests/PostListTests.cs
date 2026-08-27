using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Forum.Api.Domain;

namespace Forum.Tests;

public class PostListTests
{
    [Fact]
    public async Task A_visitor_can_list_posts_without_authenticating()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var response = await factory.CreateClient().GetAsync("/api/v1/posts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Posts_are_newest_first_by_default()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        Assert.Equal(["april", "march", "february", "january"], await TitlesAsync(factory, "/api/v1/posts"));
    }

    [Theory]
    [InlineData("newest", new[] { "april", "march", "february", "january" })]
    [InlineData("oldest", new[] { "january", "february", "march", "april" })]
    [InlineData("most-liked", new[] { "february", "march", "january", "april" })]
    public async Task Each_sort_orders_as_expected(string sort, string[] expected)
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        Assert.Equal(expected, await TitlesAsync(factory, $"/api/v1/posts?sort={sort}"));
    }

    [Fact]
    public async Task An_unknown_sort_is_refused_rather_than_passed_through()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var response = await factory.CreateClient().GetAsync("/api/v1/posts?sort=likeCount;DROP TABLE Posts");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("sort", out _));
    }

    [Fact]
    public async Task The_date_range_filter_narrows_at_both_bounds()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var titles = await TitlesAsync(
            factory,
            "/api/v1/posts?from=2026-02-01T00:00:00Z&to=2026-03-31T23:59:59Z&sort=oldest");

        Assert.Equal(["february", "march"], titles);
    }

    [Fact]
    public async Task The_author_filter_accepts_a_username()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        Assert.Equal(["march", "january"], await TitlesAsync(factory, "/api/v1/posts?author=asmith"));
        Assert.Equal(["march", "january"], await TitlesAsync(factory, "/api/v1/posts?author=ASmith"));
        Assert.Empty(await TitlesAsync(factory, "/api/v1/posts?author=nobody"));
    }

    [Fact]
    public async Task The_flag_filter_narrows_to_flagged_and_to_unflagged()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        Assert.Equal(["february"], await TitlesAsync(factory, "/api/v1/posts?flagged=true"));
        Assert.Equal(
            ["april", "march", "january"],
            await TitlesAsync(factory, "/api/v1/posts?flagged=false"));
    }

    [Fact]
    public async Task Filters_combine()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var titles = await TitlesAsync(
            factory,
            "/api/v1/posts?author=asmith&from=2026-03-01T00:00:00Z&flagged=false");

        Assert.Equal(["march"], titles);
    }

    [Fact]
    public async Task The_total_is_consistent_with_the_filter_applied()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var body = await factory.CreateClient()
            .GetFromJsonAsync<JsonElement>("/api/v1/posts?author=asmith&pageSize=1");

        Assert.Equal(2, body.GetProperty("total").GetInt32());
        Assert.Single(body.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Paging_walks_the_whole_result_without_repeating()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var first = await TitlesAsync(factory, "/api/v1/posts?sort=oldest&page=1&pageSize=2");
        var second = await TitlesAsync(factory, "/api/v1/posts?sort=oldest&page=2&pageSize=2");

        Assert.Equal(["january", "february"], first);
        Assert.Equal(["march", "april"], second);
    }

    [Fact]
    public async Task The_page_size_is_clamped_server_side()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/v1/posts?pageSize=10000");

        Assert.Equal(Forum.Api.Common.Paging.MaxPageSize, body.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task The_list_loads_no_children_to_produce_its_counts()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        var sql = await factory.WithDbAsync(db => Task.FromResult(
            db.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Id, Author = p.Author.Username, p.LikeCount, p.CommentCount })
                .ToQueryString()));

        Assert.DoesNotContain("COUNT(", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_list_reports_whether_the_caller_has_liked_each_post()
    {
        using var factory = new ForumApiFactory();
        await SeedAsync(factory);

        var liker = await PostTests.AuthenticatedClientAsync(factory, "liker");
        var target = await factory.WithDbAsync(db =>
            db.Posts.Where(p => p.Title == "april").Select(p => p.Id).SingleAsync());

        await liker.PostAsync($"/api/v1/posts/{target}/like", null);

        var body = await liker.GetFromJsonAsync<JsonElement>("/api/v1/posts?sort=newest");
        var april = body.GetProperty("items").EnumerateArray().First();

        Assert.Equal("april", april.GetProperty("title").GetString());
        Assert.True(april.GetProperty("likedByCurrentMember").GetBoolean());
    }

    private static async Task<string[]> TitlesAsync(ForumApiFactory factory, string url)
    {
        var body = await factory.CreateClient().GetFromJsonAsync<JsonElement>(url);

        return body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("title").GetString()!)
            .ToArray();
    }

    private static async Task SeedAsync(ForumApiFactory factory)
    {
        factory.CreateClient();

        await factory.WithDbAsync(async db =>
        {
            var asmith = NewMember("asmith");
            var bmokoena = NewMember("bmokoena");
            var moderator = NewMember("mod");
            db.Members.AddRange(asmith, bmokoena, moderator);

            var january = NewPost(asmith.Id, "january", new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), 3);
            var february = NewPost(bmokoena.Id, "february", new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), 9);
            var march = NewPost(asmith.Id, "march", new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), 5);
            var april = NewPost(bmokoena.Id, "april", new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), 1);

            february.IsFlagged = true;
            february.FlaggedById = moderator.Id;
            february.FlaggedAt = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);

            db.Posts.AddRange(january, february, march, april);

            await db.SaveChangesAsync();
        });
    }

    private static Member NewMember(string username) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = $"{username}@example.com",
        EmailNormalized = $"{username}@example.com",
        Username = username,
        UsernameNormalized = username,
        PasswordHash = "hash",
        Role = MemberRole.Member,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
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
