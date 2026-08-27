using System.Text;
using System.Threading.RateLimiting;
using Forum.Api.Domain;
using Forum.Api.Features.Auth;
using Forum.Api.Features.Posts;
using Forum.Api.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Forum")));

builder.Services.AddSingleton<IPasswordHasher<Member>, PasswordHasher<Member>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var credentialAttemptsPerMinute = builder.Configuration.GetValue("RateLimiting:CredentialAttemptsPerMinute", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(AuthEndpoints.CredentialsRateLimit, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = credentialAttemptsPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var v1 = app.MapGroup("/api/v1");

v1.MapGroup("/auth").WithTags("Authentication").MapAuth();
v1.MapGroup("/posts").WithTags("Posts").MapPosts();

app.Run();

public partial class Program;
