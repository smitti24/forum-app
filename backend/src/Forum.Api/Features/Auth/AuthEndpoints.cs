using System.Security.Claims;
using System.Text;
using Forum.Api.Common;
using Forum.Api.Domain;
using Forum.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Forum.Api.Features.Auth;

public static class AuthEndpoints
{
    public const string CredentialsRateLimit = "credentials";

    private const string AlreadyTaken = "That email address or username is already taken.";
    private const string BadCredentials = "That identifier and password combination is not recognised.";

    private const string DummyHash =
        "AQAAAAIAAYagAAAAELbcgxaLnGZ8h3sQKJyJx8pQEo0mCPGmC9pAPRs4bWQpCyGyDJmXeQVbxvOxaJmQpg==";

    public record RegisterRequest(string Email, string Username, string Password);

    public record LoginRequest(string Identifier, string Password);

    public record MemberSummary(Guid Id, string Username, string Email, string Role);

    public record AuthResponse(string Token, DateTime ExpiresAt, MemberSummary Member);

    public record ProfileResponse(Guid Id, string Username, string Email, string Role, DateTime CreatedAt);

    public static RouteGroupBuilder MapAuth(this RouteGroupBuilder group)
    {
        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(CredentialsRateLimit)
            .WithSummary("Register a new member and receive an access token.");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(CredentialsRateLimit)
            .WithSummary("Exchange an email address or username and password for an access token.");

        group.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .WithSummary("The calling member's own profile.");

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        ForumDbContext db,
        IPasswordHasher<Member> hasher,
        IConfiguration configuration,
        TimeProvider clock,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["An email address is required."];
        }
        else if (request.Email.Length > Identity.MaxEmailLength || !request.Email.Contains('@'))
        {
            errors["email"] = ["That does not look like an email address."];
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors["username"] = ["A username is required."];
        }
        else if (!Identity.HasNoAtSign(request.Username))
        {
            errors["username"] = ["A username may not contain '@', which is reserved for email addresses."];
        }
        else if (!Identity.IsValidUsername(request.Username))
        {
            errors["username"] =
            [
                $"A username must be {Identity.MinUsernameLength} to {Identity.MaxUsernameLength} characters, using only letters, digits, and . _ -"
            ];
        }

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < Identity.MinPasswordLength)
        {
            errors["password"] =
            [
                $"A password must be at least {Identity.MinPasswordLength} characters. Length matters more than symbols."
            ];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var emailNormalized = Identity.Normalise(request.Email);
        var usernameNormalized = Identity.Normalise(request.Username);

        var taken = await db.Members.AnyAsync(
            m => m.EmailNormalized == emailNormalized || m.UsernameNormalized == usernameNormalized, ct);

        if (taken)
        {
            return Conflict();
        }

        var member = new Member
        {
            Id = Guid.CreateVersion7(),
            Email = request.Email.Trim(),
            EmailNormalized = emailNormalized,
            Username = request.Username.Trim(),
            UsernameNormalized = usernameNormalized,
            Role = MemberRole.Member,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };

        member.PasswordHash = hasher.HashPassword(member, request.Password);

        db.Members.Add(member);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
            when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 or 1555 })
        {
            return Conflict();
        }

        return Results.Created($"/api/v1/members/{Uri.EscapeDataString(member.Username)}", Authenticate(member, configuration, clock));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ForumDbContext db,
        IPasswordHasher<Member> hasher,
        IConfiguration configuration,
        TimeProvider clock,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrEmpty(request.Password))
        {
            return Unauthorised();
        }

        var identifier = Identity.Normalise(request.Identifier);

        var member = Identity.LooksLikeEmail(identifier)
            ? await db.Members.SingleOrDefaultAsync(m => m.EmailNormalized == identifier, ct)
            : await db.Members.SingleOrDefaultAsync(m => m.UsernameNormalized == identifier, ct);

        if (member is null)
        {
            hasher.VerifyHashedPassword(new Member(), DummyHash, request.Password);
            return Unauthorised();
        }

        if (hasher.VerifyHashedPassword(member, member.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorised();
        }

        return Results.Ok(Authenticate(member, configuration, clock));
    }

    private static async Task<IResult> MeAsync(ClaimsPrincipal principal, ForumDbContext db, CancellationToken ct)
    {
        if (principal.MemberId() is not { } memberId)
        {
            return Results.Unauthorized();
        }

        var profile = await db.Members
            .Where(m => m.Id == memberId)
            .Select(m => new ProfileResponse(
                m.Id,
                m.Username,
                m.Email,
                m.Role == MemberRole.Moderator ? "moderator" : "member",
                m.CreatedAt))
            .SingleOrDefaultAsync(ct);

        return profile is null ? Results.Unauthorized() : Results.Ok(profile);
    }

    private static AuthResponse Authenticate(Member member, IConfiguration configuration, TimeProvider clock)
    {
        var (token, expiresAt) = CreateToken(member, configuration, clock);

        return new AuthResponse(
            token,
            expiresAt,
            new MemberSummary(member.Id, member.Username, member.Email, RoleName(member.Role)));
    }

    private static string RoleName(MemberRole role) => role == MemberRole.Moderator ? "moderator" : "member";

    private static IResult Conflict() =>
        Results.Problem(title: "Conflict", detail: AlreadyTaken, statusCode: StatusCodes.Status409Conflict);

    private static IResult Unauthorised() =>
        Results.Problem(title: "Unauthenticated", detail: BadCredentials, statusCode: StatusCodes.Status401Unauthorized);

    private static (string Token, DateTime ExpiresAt) CreateToken(
        Member member,
        IConfiguration configuration,
        TimeProvider clock)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var lifetime = configuration.GetValue("Jwt:LifetimeMinutes", 60);
        var expiresAt = clock.GetUtcNow().UtcDateTime.AddMinutes(lifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
                new Claim(ClaimTypes.Name, member.Username),
                new Claim(ClaimTypes.Role, RoleName(member.Role))
            ]),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
