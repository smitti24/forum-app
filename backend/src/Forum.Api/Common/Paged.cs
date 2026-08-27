namespace Forum.Api.Common;

public record Paged<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public record AuthorSummary(Guid Id, string Username);

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Clamp(int? page, int? pageSize) =>
        (Math.Max(page ?? 1, 1),
            Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}
