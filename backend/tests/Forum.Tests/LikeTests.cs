using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Forum.Tests;

public class LikeTests
{
    [Fact]
    public async Task A_member_can_like_another_members_post()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var liker = await PostTests.AuthenticatedClientAsync(factory, "liker");
        var postId = await PostTests.CreatePostAsync(author);

        var response = await Like(liker, postId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("likeCount").GetInt32());
        Assert.True(body.GetProperty("likedByCurrentMember").GetBoolean());
    }

    [Fact]
    public async Task A_member_cannot_like_the_same_post_twice()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var liker = await PostTests.AuthenticatedClientAsync(factory, "liker");
        var postId = await PostTests.CreatePostAsync(author);

        await Like(liker, postId);
        var second = await Like(liker, postId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var post = await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");
        Assert.Equal(1, post.GetProperty("likeCount").GetInt32());
    }

    [Fact]
    public async Task A_member_cannot_like_their_own_post()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var postId = await PostTests.CreatePostAsync(author);

        var response = await Like(author, postId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var post = await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");
        Assert.Equal(0, post.GetProperty("likeCount").GetInt32());
    }

    [Fact]
    public async Task A_member_can_remove_a_like()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var liker = await PostTests.AuthenticatedClientAsync(factory, "liker");
        var postId = await PostTests.CreatePostAsync(author);

        await Like(liker, postId);
        var response = await Unlike(liker, postId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("likeCount").GetInt32());
        Assert.False(body.GetProperty("likedByCurrentMember").GetBoolean());
    }

    [Fact]
    public async Task Unliking_a_post_that_was_never_liked_is_refused_cleanly()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var liker = await PostTests.AuthenticatedClientAsync(factory, "liker");
        var postId = await PostTests.CreatePostAsync(author);

        var response = await Unlike(liker, postId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task An_unauthenticated_caller_cannot_like_or_unlike()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var postId = await PostTests.CreatePostAsync(author);
        var visitor = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await Like(visitor, postId)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Unlike(visitor, postId)).StatusCode);
    }

    [Fact]
    public async Task Liking_a_missing_post_is_not_found()
    {
        using var factory = new ForumApiFactory();
        var liker = await PostTests.AuthenticatedClientAsync(factory, "liker");

        var response = await Like(liker, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_read_reports_whether_the_caller_has_liked_the_post()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var liker = await PostTests.AuthenticatedClientAsync(factory, "liker");
        var other = await PostTests.AuthenticatedClientAsync(factory, "other");
        var postId = await PostTests.CreatePostAsync(author);

        await Like(liker, postId);

        var forLiker = await liker.GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");
        var forOther = await other.GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");
        var forVisitor = await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");

        Assert.True(forLiker.GetProperty("likedByCurrentMember").GetBoolean());
        Assert.False(forOther.GetProperty("likedByCurrentMember").GetBoolean());
        Assert.False(forVisitor.GetProperty("likedByCurrentMember").GetBoolean());
    }

    [Fact]
    public async Task The_like_count_reflects_likes_from_several_members()
    {
        using var factory = new ForumApiFactory();
        var author = await PostTests.AuthenticatedClientAsync(factory, "author");
        var first = await PostTests.AuthenticatedClientAsync(factory, "first");
        var second = await PostTests.AuthenticatedClientAsync(factory, "second");
        var postId = await PostTests.CreatePostAsync(author);

        await Like(first, postId);
        await Like(second, postId);

        var post = await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/api/v1/posts/{postId}");

        Assert.Equal(2, post.GetProperty("likeCount").GetInt32());
    }

    private static Task<HttpResponseMessage> Like(HttpClient client, Guid postId) =>
        client.PostAsync($"/api/v1/posts/{postId}/like", null);

    private static Task<HttpResponseMessage> Unlike(HttpClient client, Guid postId) =>
        client.DeleteAsync($"/api/v1/posts/{postId}/like");
}
