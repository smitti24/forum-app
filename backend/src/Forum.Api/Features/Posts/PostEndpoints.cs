using System.Security.Claims;
using Forum.Api.Common;
using Forum.Api.Domain;
using Forum.Api.Features.Comments;
using Forum.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Posts;

public static class PostEndpoints
{
    public const int MaxTitleLength = 200;
    public const int MaxBodyLength = 10_000;

    public record CreatePostRequest(string Title, string Body);

    public record FlagSummary(string FlaggedBy, DateTime FlaggedAt);

    public record PostResponse(
        Guid Id,
        string Title,
        string Body,
        AuthorSummary Author,
        DateTime CreatedAt,
        int LikeCount,
        int CommentCount,
        bool LikedByCurrentMember,
        FlagSummary? Flag);

    public record PostDetailResponse(
        Guid Id,
        string Title,
        string Body,
        AuthorSummary Author,
        DateTime CreatedAt,
        int LikeCount,
        int CommentCount,
        bool LikedByCurrentMember,
        FlagSummary? Flag,
        Paged<CommentEndpoints.CommentResponse> Comments);

    public static RouteGroupBuilder MapPosts(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateAsync)
            .RequireAuthorization()
            .WithSummary("Create a post.");

        group.MapGet("/{id:guid}", GetAsync)
            .AllowAnonymous()
            .WithSummary("Read a single post with its first page of comments.");

        return group;
    }

    private static async Task<IResult> CreateAsync(
        CreatePostRequest request,
        ClaimsPrincipal principal,
        ForumDbContext db,
        TimeProvider clock,
        CancellationToken ct)
    {
        if (principal.MemberId() is not { } memberId || principal.Username() is not { } username)
        {
            return Results.Unauthorized();
        }

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["title"] = ["A title is required."];
        }
        else if (request.Title.Trim().Length > MaxTitleLength)
        {
            errors["title"] = [$"A title may be at most {MaxTitleLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            errors["body"] = ["A body is required."];
        }
        else if (request.Body.Trim().Length > MaxBodyLength)
        {
            errors["body"] = [$"A body may be at most {MaxBodyLength} characters."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var post = new Post
        {
            Id = Guid.CreateVersion7(),
            AuthorId = memberId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);

        var response = new PostResponse(
            post.Id,
            post.Title,
            post.Body,
            new AuthorSummary(memberId, username),
            post.CreatedAt,
            post.LikeCount,
            post.CommentCount,
            LikedByCurrentMember: false,
            Flag: null);

        return Results.Created($"/api/v1/posts/{post.Id}", response);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        ForumDbContext db,
        CancellationToken ct)
    {
        var memberId = principal.MemberId();

        var post = await db.Posts
            .Where(p => p.Id == id)
            .Select(p => new PostResponse(
                p.Id,
                p.Title,
                p.Body,
                new AuthorSummary(p.AuthorId, p.Author.Username),
                p.CreatedAt,
                p.LikeCount,
                p.CommentCount,
                memberId != null && p.Likes.Any(l => l.MemberId == memberId),
                p.IsFlagged ? new FlagSummary(p.FlaggedBy!.Username, p.FlaggedAt!.Value) : null))
            .SingleOrDefaultAsync(ct);

        if (post is null)
        {
            return NotFound();
        }

        var comments = await CommentEndpoints.PageAsync(db, id, page: null, pageSize: null, ct);

        return Results.Ok(new PostDetailResponse(
            post.Id,
            post.Title,
            post.Body,
            post.Author,
            post.CreatedAt,
            post.LikeCount,
            post.CommentCount,
            post.LikedByCurrentMember,
            post.Flag,
            comments));
    }

    internal static IResult NotFound() =>
        Results.Problem(title: "Not found", detail: "No such post.", statusCode: StatusCodes.Status404NotFound);
}
