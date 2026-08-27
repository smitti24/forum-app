using System.Security.Claims;

namespace Forum.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid? MemberId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static string? Username(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Name);
}
