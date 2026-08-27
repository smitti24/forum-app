using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Forum.Tests;

public class FlagTests
{
    [Fact]
    public async Task A_moderator_can_flag_a_post()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var moderator = await ModeratorClientAsync(factory, "mod");
        var postId = await PostTests.CreatePostAsync(author);

        var response = await Flag(moderator, postId, "Contradicts the published documentation.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isFlagged").GetBoolean());
        Assert.Equal("mod", body.GetProperty("flaggedBy").GetString());
        Assert.Equal("Contradicts the published documentation.", body.GetProperty("note").GetString());
    }

    [Fact]
    public async Task A_flagged_post_reports_who_flagged_it_and_when()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var moderator = await ModeratorClientAsync(factory, "mod");
        var postId = await PostTests.CreatePostAsync(author);

        await Flag(moderator, postId);

        var post = await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");
        var flag = post.GetProperty("flag");

        Assert.Equal(JsonValueKind.Object, flag.ValueKind);
        Assert.Equal("mod", flag.GetProperty("flaggedBy").GetString());
        Assert.EndsWith("Z", flag.GetProperty("flaggedAt").GetString()!);
    }

    [Fact]
    public async Task A_moderator_can_remove_a_flag()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var moderator = await ModeratorClientAsync(factory, "mod");
        var postId = await PostTests.CreatePostAsync(author);

        await Flag(moderator, postId);
        var response = await moderator.DeleteAsync($"/api/v1/posts/{postId}/flag");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var post = await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");
        Assert.Equal(JsonValueKind.Null, post.GetProperty("flag").ValueKind);
    }

    [Fact]
    public async Task An_ordinary_member_cannot_flag_or_unflag()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var member = await PostTests.AuthenticatedClientAsync(factory, "member");
        var postId = await PostTests.CreatePostAsync(author);

        Assert.Equal(HttpStatusCode.Forbidden, (await Flag(member, postId)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await member.DeleteAsync($"/api/v1/posts/{postId}/flag")).StatusCode);
    }

    [Fact]
    public async Task An_unauthenticated_caller_cannot_flag()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var postId = await PostTests.CreatePostAsync(author);

        var response = await Flag(factory.CreateClient(), postId);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Flagging_a_missing_post_is_not_found()
    {
        using var factory = new ForumApiFactory();
        var moderator = await ModeratorClientAsync(factory, "mod");

        var response = await Flag(moderator, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_flagged_post_stays_readable_and_still_accepts_comments_and_likes()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var moderator = await ModeratorClientAsync(factory, "mod");
        var member = await PostTests.AuthenticatedClientAsync(factory, "member");
        var postId = await PostTests.CreatePostAsync(author);

        await Flag(moderator, postId);

        var read = await factory.CreateClient().GetAsync($"/api/v1/posts/{postId}");
        var comment = await member.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new { body = "Still open." });
        var like = await member.PostAsync($"/api/v1/posts/{postId}/like", null);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, like.StatusCode);
    }

    [Fact]
    public async Task A_moderator_retains_ordinary_member_abilities()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var moderator = await ModeratorClientAsync(factory, "mod");
        var postId = await PostTests.CreatePostAsync(author);

        var ownPost = await PostTests.CreatePostAsync(moderator, "A moderator's own post");
        var comment = await moderator.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new { body = "Noted." });
        var like = await moderator.PostAsync($"/api/v1/posts/{postId}/like", null);

        Assert.NotEqual(Guid.Empty, ownPost);
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, like.StatusCode);
    }

    [Fact]
    public async Task An_over_long_note_is_refused()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var moderator = await ModeratorClientAsync(factory, "mod");
        var postId = await PostTests.CreatePostAsync(author);

        var response = await Flag(moderator, postId, new string('a', 1_001));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Flag(HttpClient client, Guid postId, string? note = null) =>
        client.PostAsJsonAsync($"/api/v1/posts/{postId}/flag", new { note });

    internal static async Task<HttpClient> ModeratorClientAsync(ForumApiFactory factory, string username)
    {
        var client = factory.CreateClient();
        var token = await factory.RegisterModeratorAndLoginAsync(client, username);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
