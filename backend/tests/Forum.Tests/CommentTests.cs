using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Forum.Api.Features.Comments;

namespace Forum.Tests;

public class CommentTests
{
    [Fact]
    public async Task A_member_can_comment_on_a_post()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);

        var response = await Comment(client, postId, "That depends on the failure mode.");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("That depends on the failure mode.", body.GetProperty("body").GetString());
        Assert.Equal("asmith", body.GetProperty("author").GetProperty("username").GetString());
        Assert.Equal(postId, body.GetProperty("postId").GetGuid());
    }

    [Fact]
    public async Task An_unauthenticated_caller_cannot_comment()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(author);

        var response = await Comment(factory.CreateClient(), postId, "A comment.");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Commenting_on_a_missing_post_is_not_found()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");

        var response = await Comment(client, Guid.CreateVersion7(), "A comment.");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_visitor_can_read_comments_without_authenticating()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);
        await Comment(client, postId, "A comment.");

        var response = await factory.CreateClient().GetAsync($"/api/v1/posts/{postId}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Comments_are_paged_oldest_first_with_a_total()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);

        for (var i = 1; i <= 5; i++)
        {
            await Comment(client, postId, $"comment {i}");
        }

        var response = await client.GetAsync($"/api/v1/posts/{postId}/comments?page=1&pageSize=2");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(5, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(2, body.GetProperty("pageSize").GetInt32());

        var bodies = body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("body").GetString()!)
            .ToArray();

        Assert.Equal(["comment 1", "comment 2"], bodies);

        var secondPage = await client.GetAsync($"/api/v1/posts/{postId}/comments?page=2&pageSize=2");
        var secondBody = await secondPage.Content.ReadFromJsonAsync<JsonElement>();

        var secondBodies = secondBody.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("body").GetString()!)
            .ToArray();

        Assert.Equal(["comment 3", "comment 4"], secondBodies);
    }

    [Fact]
    public async Task The_page_size_is_clamped_server_side()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);

        var response = await client.GetAsync($"/api/v1/posts/{postId}/comments?pageSize=10000");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(Forum.Api.Common.Paging.MaxPageSize, body.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task The_comment_count_on_the_post_matches_the_total()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);

        await Comment(client, postId, "one");
        await Comment(client, postId, "two");
        await Comment(client, postId, "three");

        var post = await client.GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");

        Assert.Equal(3, post.GetProperty("commentCount").GetInt32());
        Assert.Equal(3, post.GetProperty("comments").GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Post_detail_embeds_the_first_page_of_comments()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);
        await Comment(client, postId, "the only comment");

        var post = await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");
        var comments = post.GetProperty("comments");

        Assert.Equal(1, comments.GetProperty("total").GetInt32());
        Assert.Equal(
            "the only comment",
            comments.GetProperty("items")[0].GetProperty("body").GetString());
    }

    [Fact]
    public async Task An_empty_comment_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);

        var response = await Comment(client, postId, "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("body", out _));
    }

    [Fact]
    public async Task An_over_long_comment_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var postId = await PostTests.CreatePostAsync(client);

        var response = await Comment(client, postId, new string('a', CommentEndpoints.MaxBodyLength + 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Comment(HttpClient client, Guid postId, string body) =>
        client.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new { body });
}
