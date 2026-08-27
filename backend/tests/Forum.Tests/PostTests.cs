using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Forum.Api.Domain;
using Forum.Api.Features.Posts;

namespace Forum.Tests;

public class PostTests
{
    [Fact]
    public async Task A_member_can_create_a_post()
    {
        using var factory = new ForumApiFactory();
        var client = await AuthenticatedClientAsync(factory, "asmith");

        var response = await client.PostAsJsonAsync("/api/v1/posts", new
        {
            title = "Liveness checks and retries",
            body = "What is the correct retry policy for a failed liveness check?"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Liveness checks and retries", body.GetProperty("title").GetString());
        Assert.Equal("asmith", body.GetProperty("author").GetProperty("username").GetString());
        Assert.Equal(0, body.GetProperty("likeCount").GetInt32());
        Assert.Equal(0, body.GetProperty("commentCount").GetInt32());
        Assert.False(body.GetProperty("likedByCurrentMember").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("flag").ValueKind);
    }

    [Fact]
    public async Task An_unauthenticated_caller_cannot_create_a_post()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/posts", new
        {
            title = "title",
            body = "body"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_visitor_can_read_a_post_without_authenticating()
    {
        using var factory = new ForumApiFactory();
        var author = await AuthenticatedClientAsync(factory, "asmith");
        var id = await CreatePostAsync(author);

        var visitor = factory.CreateClient();
        var response = await visitor.GetAsync($"/api/v1/posts/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("asmith", body.GetProperty("author").GetProperty("username").GetString());
    }

    [Fact]
    public async Task A_missing_post_is_not_found()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/posts/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_post_never_exposes_the_authors_email()
    {
        using var factory = new ForumApiFactory();
        var author = await AuthenticatedClientAsync(factory, "asmith");
        var id = await CreatePostAsync(author);

        var response = await factory.CreateClient().GetAsync($"/api/v1/posts/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("asmith@example.com", body);
        Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "body", "title")]
    [InlineData("   ", "body", "title")]
    [InlineData("title", "", "body")]
    [InlineData("title", "   ", "body")]
    public async Task A_validation_failure_names_the_field_that_caused_it(
        string title,
        string postBody,
        string expectedField)
    {
        using var factory = new ForumApiFactory();
        var client = await AuthenticatedClientAsync(factory, "asmith");

        var response = await client.PostAsJsonAsync("/api/v1/posts", new { title, body = postBody });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(expectedField, out _));
    }

    [Fact]
    public async Task An_over_long_title_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = await AuthenticatedClientAsync(factory, "asmith");

        var response = await client.PostAsJsonAsync("/api/v1/posts", new
        {
            title = new string('a', PostEndpoints.MaxTitleLength + 1),
            body = "body"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reading_a_post_issues_one_query_and_loads_no_children()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        var sql = await factory.WithDbAsync(db => Task.FromResult(
            db.Posts
                .Where(p => p.Id == Guid.Empty)
                .Select(p => new
                {
                    p.Id,
                    Author = p.Author.Username,
                    p.LikeCount,
                    p.CommentCount
                })
                .ToQueryString()));

        Assert.DoesNotContain("COUNT(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("JOIN", sql);
    }

    [Fact]
    public async Task Counts_are_read_from_the_post_not_counted_from_children()
    {
        using var factory = new ForumApiFactory();
        var author = await AuthenticatedClientAsync(factory, "asmith");
        var id = await CreatePostAsync(author);

        await factory.WithDbAsync(async db =>
        {
            var post = await db.Posts.SingleAsync(p => p.Id == id);
            post.LikeCount = 7;
            post.CommentCount = 3;
            await db.SaveChangesAsync();
        });

        var response = await factory.CreateClient().GetAsync($"/api/v1/posts/{id}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(7, body.GetProperty("likeCount").GetInt32());
        Assert.Equal(3, body.GetProperty("commentCount").GetInt32());
    }

    private static int CountOccurrences(string text, string term)
    {
        var count = 0;
        var index = text.IndexOf(term, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = text.IndexOf(term, index + term.Length, StringComparison.Ordinal);
        }

        return count;
    }

    internal static async Task<HttpClient> AuthenticatedClientAsync(ForumApiFactory factory, string username)
    {
        var client = factory.CreateClient();
        var token = await factory.RegisterAndLoginAsync(client, username);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    internal static async Task<Guid> CreatePostAsync(HttpClient client, string title = "A title")
    {
        var response = await client.PostAsJsonAsync("/api/v1/posts", new
        {
            title,
            body = "A body with enough substance to be worth reading."
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }
}
