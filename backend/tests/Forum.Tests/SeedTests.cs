using System.Net.Http.Json;
using System.Text.Json;
using Forum.Api.Domain;
using Forum.Api.Persistence;

namespace Forum.Tests;

public class SeedTests
{
    private sealed class SeededFactory : ForumApiFactory
    {
        protected override bool Seed => true;
    }

    [Fact]
    public async Task Seeding_creates_accounts_covering_both_roles()
    {
        using var factory = new SeededFactory();
        factory.CreateClient();

        var roles = await factory.WithDbAsync(db =>
            db.Members.OrderBy(m => m.Username).Select(m => new { m.Username, m.Role }).ToListAsync());

        Assert.Equal(4, roles.Count);
        Assert.Contains(roles, r => r.Role == MemberRole.Moderator);
        Assert.Equal(3, roles.Count(r => r.Role == MemberRole.Member));
    }

    [Fact]
    public async Task Every_seeded_account_can_log_in_with_the_documented_password()
    {
        using var factory = new SeededFactory();
        var client = factory.CreateClient();

        foreach (var username in (string[])["asmith", "bmokoena", "moderator", "dubious"])
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                identifier = username,
                password = Seeder.Password
            });

            Assert.True(response.IsSuccessStatusCode, $"{username} could not log in");
        }
    }

    [Fact]
    public async Task Seeded_content_includes_a_flagged_post()
    {
        using var factory = new SeededFactory();
        var client = factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/posts?flagged=true");

        Assert.Equal(1, body.GetProperty("total").GetInt32());

        var flag = body.GetProperty("items")[0].GetProperty("flag");
        Assert.Equal("moderator", flag.GetProperty("flaggedBy").GetString());
    }

    [Fact]
    public async Task Seeded_counts_match_the_rows_they_denormalise()
    {
        using var factory = new SeededFactory();
        factory.CreateClient();

        await factory.WithDbAsync(async db =>
        {
            foreach (var post in await db.Posts.ToListAsync())
            {
                var comments = await db.Comments.CountAsync(c => c.PostId == post.Id);
                var likes = await db.Likes.CountAsync(l => l.PostId == post.Id);

                Assert.Equal(comments, post.CommentCount);
                Assert.Equal(likes, post.LikeCount);
            }
        });
    }

    [Fact]
    public async Task Seeding_is_idempotent()
    {
        using var factory = new SeededFactory();
        factory.CreateClient();

        await factory.Services.SeedAsync();

        var members = await factory.WithDbAsync(db => db.Members.CountAsync());

        Assert.Equal(4, members);
    }

    [Fact]
    public async Task There_is_enough_seeded_content_to_page()
    {
        using var factory = new SeededFactory();
        var client = factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/posts?pageSize=5");

        Assert.True(body.GetProperty("total").GetInt32() > 5);
        Assert.Equal(5, body.GetProperty("items").GetArrayLength());
    }
}
