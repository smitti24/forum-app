using System.Security.Claims;
using Forum.Api.Common;
using Forum.Api.Domain;
using Forum.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Comments;

public static class CommentEndpoints
{
    public const int MaxBodyLength = 5_000;

    public record CreateCommentRequest(string Body);

    public record CommentResponse(Guid Id, Guid PostId, AuthorSummary Author, string Body, DateTime CreatedAt);

    public static RouteGroupBuilder MapComments(this RouteGroupBuilder group)
    {
        group.MapPost("/{postId:guid}/comments", CreateAsync)
            .RequireAuthorization()
            .WithSummary("Comment on a post.");

        group.MapGet("/{postId:guid}/comments", ListAsync)
            .AllowAnonymous()
            .WithSummary("Read a post's comments, oldest first.");

        return group;
    }

    private static async Task<IResult> CreateAsync(
        Guid postId,
        CreateCommentRequest request,
        ClaimsPrincipal principal,
        ForumDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        if (principal.MemberId() is not { } memberId || principal.Username() is not { } username)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["A comment cannot be empty."]
            });
        }

        if (request.Body.Trim().Length > MaxBodyLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = [$"A comment may be at most {MaxBodyLength} characters."]
            });
        }

        var post = await db.Posts.SingleOrDefaultAsync(p => p.Id == postId, ct);

        if (post is null)
        {
            return PostNotFound();
        }

        var comment = new Comment
        {
            Id = Guid.CreateVersion7(),
            PostId = postId,
            AuthorId = memberId,
            Body = request.Body.Trim(),
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        db.Comments.Add(comment);
        post.CommentCount++;

        await db.SaveChangesAsync(ct);

        var response = new CommentResponse(
            comment.Id,
            postId,
            new AuthorSummary(memberId, username),
            comment.Body,
            comment.CreatedAt);

        return Results.Created($"/api/v1/posts/{postId}/comments/{comment.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        Guid postId,
        ForumDbContext db,
        CancellationToken ct,
        int? page = null,
        int? pageSize = null)
    {
        if (!await db.Posts.AnyAsync(p => p.Id == postId, ct))
        {
            return PostNotFound();
        }

        return Results.Ok(await PageAsync(db, postId, page, pageSize, ct));
    }

    internal static async Task<Paged<CommentResponse>> PageAsync(
        ForumDbContext db,
        Guid postId,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        var (resolvedPage, resolvedPageSize) = Paging.Clamp(page, pageSize);

        var query = db.Comments.Where(c => c.PostId == postId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Skip((resolvedPage - 1) * resolvedPageSize)
            .Take(resolvedPageSize)
            .Select(c => new CommentResponse(
                c.Id,
                c.PostId,
                new AuthorSummary(c.AuthorId, c.Author.Username),
                c.Body,
                c.CreatedAt))
            .ToListAsync(ct);

        return new Paged<CommentResponse>(items, resolvedPage, resolvedPageSize, total);
    }

    private static IResult PostNotFound() =>
        Results.Problem(title: "Not found", detail: "No such post.", statusCode: StatusCodes.Status404NotFound);
}
