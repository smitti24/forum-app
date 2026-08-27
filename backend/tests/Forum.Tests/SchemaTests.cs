using Forum.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forum.Tests;

public class SchemaTests
{
    [Fact]
    public async Task Usernames_differing_only_by_case_cannot_both_exist()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        await factory.WithDbAsync(async db =>
        {
            db.Members.Add(NewMember("asmith", "asmith@example.com"));
            await db.SaveChangesAsync();
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => factory.WithDbAsync(async db =>
        {
            db.Members.Add(NewMember("asmith", "different@example.com"));
            await db.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Emails_differing_only_by_case_cannot_both_exist()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        await factory.WithDbAsync(async db =>
        {
            db.Members.Add(NewMember("asmith", "asmith@example.com"));
            await db.SaveChangesAsync();
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => factory.WithDbAsync(async db =>
        {
            db.Members.Add(NewMember("different", "asmith@example.com"));
            await db.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task A_member_cannot_like_the_same_post_twice()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        Guid postId = default;
        Guid memberId = default;

        await factory.WithDbAsync(async db =>
        {
            var author = NewMember("author", "author@example.com");
            var liker = NewMember("liker", "liker@example.com");
            db.Members.AddRange(author, liker);

            var post = new Post
            {
                Id = Guid.CreateVersion7(),
                AuthorId = author.Id,
                Title = "title",
                Body = "body",
                CreatedAt = DateTime.UtcNow
            };
            db.Posts.Add(post);

            db.Likes.Add(new Like { PostId = post.Id, MemberId = liker.Id, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            postId = post.Id;
            memberId = liker.Id;
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => factory.WithDbAsync(async db =>
        {
            db.Likes.Add(new Like { PostId = postId, MemberId = memberId, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }));
    }

    private static Member NewMember(string username, string email) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = email,
        EmailNormalized = email.ToLowerInvariant(),
        Username = username,
        UsernameNormalized = username.ToLowerInvariant(),
        PasswordHash = "hash",
        Role = MemberRole.Member,
        CreatedAt = DateTime.UtcNow
    };
}
