using System.Security.Claims;
using Forum.Api.Common;
using Forum.Api.Domain;
using Forum.Api.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Likes;

public static class LikeEndpoints
{
    public record LikeResponse(Guid PostId, int LikeCount, bool LikedByCurrentMember);

    public static RouteGroupBuilder MapLikes(this RouteGroupBuilder group)
    {
        group.MapPost("/{postId:guid}/like", LikeAsync)
            .RequireAuthorization()
            .WithSummary("Like a post. A member may like a post once, and never their own.");

        group.MapDelete("/{postId:guid}/like", UnlikeAsync)
            .RequireAuthorization()
            .WithSummary("Remove a like.");

        return group;
    }

    private static async Task<IResult> LikeAsync(
        Guid postId,
        ClaimsPrincipal principal,
        ForumDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        if (principal.MemberId() is not { } memberId)
        {
            return Results.Unauthorized();
        }

        var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId, ct);

        if (post is null)
        {
            return PostNotFound();
        }

        if (post.AuthorId == memberId)
        {
            return Results.Problem(
                title: "Forbidden",
                detail: "A member may not like their own post.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        db.Likes.Add(new Like { PostId = postId, MemberId = memberId, CreatedAt = clock.GetUtcNow().UtcDateTime });
        post.LikeCount++;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
            when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 })
        {
            return Results.Problem(
                title: "Conflict",
                detail: "You have already liked this post.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(new LikeResponse(postId, post.LikeCount, LikedByCurrentMember: true));
    }

    private static async Task<IResult> UnlikeAsync(
        Guid postId,
        ClaimsPrincipal principal,
        ForumDbContext db,
        CancellationToken ct)
    {
        if (principal.MemberId() is not { } memberId)
        {
            return Results.Unauthorized();
        }

        var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId, ct);

        if (post is null)
        {
            return PostNotFound();
        }

        var like = await db.Likes.SingleOrDefaultAsync(l => l.PostId == postId && l.MemberId == memberId, ct);

        if (like is null)
        {
            return Results.Problem(
                title: "Conflict",
                detail: "You have not liked this post.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Likes.Remove(like);
        post.LikeCount = Math.Max(0, post.LikeCount - 1);

        await db.SaveChangesAsync(ct);

        return Results.Ok(new LikeResponse(postId, post.LikeCount, LikedByCurrentMember: false));
    }

    private static IResult PostNotFound() =>
        Results.Problem(title: "Not found", detail: "No such post.", statusCode: StatusCodes.Status404NotFound);
}
