using Forum.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Persistence;

public static class Seeder
{
    public const string Password = "forum-demo-password";

    public static async Task SeedAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Member>>();

        if (await db.Members.AnyAsync(ct))
        {
            return;
        }

        var start = new DateTime(2026, 5, 4, 9, 0, 0, DateTimeKind.Utc);

        var asmith = NewMember(hasher, "asmith", MemberRole.Member, start);
        var bmokoena = NewMember(hasher, "bmokoena", MemberRole.Member, start.AddMinutes(5));
        var moderator = NewMember(hasher, "moderator", MemberRole.Moderator, start.AddMinutes(10));
        var dubious = NewMember(hasher, "dubious", MemberRole.Member, start.AddMinutes(15));

        db.Members.AddRange(asmith, bmokoena, moderator, dubious);

        var posts = new[]
        {
            NewPost(asmith, "Retry policy for a failed liveness check", "What is the correct backoff when a liveness check returns a transient failure? We are seeing duplicate submissions when the client retries immediately.", start.AddHours(1)),
            NewPost(bmokoena, "Webhook signature verification", "The signature header does not match what we compute. Are we meant to sign the raw request body, or the parsed and re-serialised payload?", start.AddHours(3)),
            NewPost(dubious, "Liveness checks work fine on a printed photograph", "In our testing the liveness check accepts a printed photograph about half the time, so it is not really doing anything and can be skipped in production.", start.AddHours(5)),
            NewPost(asmith, "Sandbox credentials expire silently", "Our sandbox key stopped working with no warning and no error that said so. Is there a documented rotation period we should be scheduling around?", start.AddHours(8)),
            NewPost(bmokoena, "Document capture on low-end Android", "Capture quality drops sharply below 2GB of RAM. Is there published guidance on minimum device specifications?", start.AddHours(11)),
            NewPost(moderator, "Rate limits, per environment", "Sandbox allows sixty requests a minute and production is negotiated per contract. Back off on 429 rather than retrying immediately.", start.AddHours(14)),
            NewPost(asmith, "Handling partial verification results", "When a check returns partial, should we treat it as a failure or prompt the user to retry the capture step?", start.AddHours(18)),
        };

        db.Posts.AddRange(posts);

        var flagged = posts[2];
        flagged.IsFlagged = true;
        flagged.FlaggedById = moderator.Id;
        flagged.FlaggedAt = start.AddHours(6);
        flagged.FlagNote = "Misleading. Liveness detection is not bypassed by a printed photograph; see the published test results.";

        db.Comments.AddRange(
            NewComment(posts[0], bmokoena, "Exponential backoff starting at two seconds worked for us, capped at five attempts.", start.AddHours(2)),
            NewComment(posts[0], moderator, "Key your retries on the idempotency header, otherwise duplicates are expected behaviour rather than a bug.", start.AddHours(2).AddMinutes(20)),
            NewComment(posts[1], asmith, "Raw body, before any parsing. Re-serialising reorders keys and the signature will never match.", start.AddHours(4)),
            NewComment(posts[2], moderator, "This is not correct and the post has been flagged. Printed photographs are rejected in every published test.", start.AddHours(6).AddMinutes(5)),
            NewComment(posts[4], asmith, "We set a floor at 3GB and fall back to server-side capture below that.", start.AddHours(12)),
            NewComment(posts[6], bmokoena, "We prompt for a retry once, then fall back to manual review.", start.AddHours(19)));

        posts[0].CommentCount = 2;
        posts[1].CommentCount = 1;
        posts[2].CommentCount = 1;
        posts[4].CommentCount = 1;
        posts[6].CommentCount = 1;

        AddLikes(db, posts[0], start, bmokoena, moderator, dubious);
        AddLikes(db, posts[1], start, asmith, moderator);
        AddLikes(db, posts[3], start, bmokoena);
        AddLikes(db, posts[5], start, asmith, bmokoena, dubious);
        AddLikes(db, posts[6], start, bmokoena, moderator);

        await db.SaveChangesAsync(ct);
    }

    private static void AddLikes(ForumDbContext db, Post post, DateTime at, params Member[] members)
    {
        foreach (var member in members)
        {
            db.Likes.Add(new Like { PostId = post.Id, MemberId = member.Id, CreatedAt = at });
        }

        post.LikeCount = members.Length;
    }

    private static Member NewMember(IPasswordHasher<Member> hasher, string username, MemberRole role, DateTime createdAt)
    {
        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            Email = $"{username}@example.com",
            EmailNormalized = $"{username}@example.com",
            Username = username,
            UsernameNormalized = username,
            Role = role,
            CreatedAt = createdAt
        };

        member.PasswordHash = hasher.HashPassword(member, Password);

        return member;
    }

    private static Post NewPost(Member author, string title, string body, DateTime createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        AuthorId = author.Id,
        Title = title,
        Body = body,
        CreatedAt = createdAt
    };

    private static Comment NewComment(Post post, Member author, string body, DateTime createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        PostId = post.Id,
        AuthorId = author.Id,
        Body = body,
        CreatedAt = createdAt
    };
}
