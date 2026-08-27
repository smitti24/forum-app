using System.Text.RegularExpressions;

namespace Forum.Api.Domain;

public static partial class Identity
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 32;
    public const int MaxEmailLength = 320;
    public const int MinPasswordLength = 12;

    [GeneratedRegex(@"^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedUsername();

    public static string Normalise(string value) => value.Trim().ToLowerInvariant();

    public static bool LooksLikeEmail(string identifier) => identifier.Contains('@');

    public static bool HasNoAtSign(string username) => !username.Contains('@');

    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        var trimmed = username.Trim();

        return trimmed.Length >= MinUsernameLength
               && trimmed.Length <= MaxUsernameLength
               && AllowedUsername().IsMatch(trimmed);
    }
}
