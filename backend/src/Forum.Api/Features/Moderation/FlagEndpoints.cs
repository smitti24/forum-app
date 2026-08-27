using System.Security.Claims;
using Forum.Api.Common;
using Forum.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Moderation;

public static class FlagEndpoints
{
    public const int MaxNoteLength = 1_000;

    public record FlagRequest(string? Note);

    public record FlagResponse(Guid PostId, bool IsFlagged, string? FlaggedBy, DateTime? FlaggedAt, string? Note);

    public static RouteGroupBuilder MapFlags(this RouteGroupBuilder group)
    {
        group.MapPost("/{postId:guid}/flag", FlagAsync)
            .RequireAuthorization(policy => policy.RequireRole("moderator"))
            .WithSummary("Flag a post as containing misleading or false information. Moderators only.");

        group.MapDelete("/{postId:guid}/flag", UnflagAsync)
            .RequireAuthorization(policy => policy.RequireRole("moderator"))
            .WithSummary("Remove a flag. Moderators only.");

        return group;
    }

    private static async Task<IResult> FlagAsync(
        Guid postId,
        FlagRequest? request,
        ClaimsPrincipal principal,
        ForumDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        if (principal.MemberId() is not { } moderatorId || principal.Username() is not { } moderator)
        {
            return Results.Unauthorized();
        }

        var note = request?.Note?.Trim();

        if (note is { Length: > MaxNoteLength })
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["note"] = [$"A note may be at most {MaxNoteLength} characters."]
            });
        }

        var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId, ct);

        if (post is null)
        {
            return PostNotFound();
        }

        post.IsFlagged = true;
        post.FlaggedById = moderatorId;
        post.FlaggedAt = clock.GetUtcNow().UtcDateTime;
        post.FlagNote = string.IsNullOrWhiteSpace(note) ? null : note;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new FlagResponse(postId, true, moderator, post.FlaggedAt, post.FlagNote));
    }

    private static async Task<IResult> UnflagAsync(
        Guid postId,
        ForumDbContext db,
        CancellationToken ct)
    {
        var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId, ct);

        if (post is null)
        {
            return PostNotFound();
        }

        post.IsFlagged = false;
        post.FlaggedById = null;
        post.FlaggedAt = null;
        post.FlagNote = null;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new FlagResponse(postId, false, null, null, null));
    }

    private static IResult PostNotFound() =>
        Results.Problem(title: "Not found", detail: "No such post.", statusCode: StatusCodes.Status404NotFound);
}
