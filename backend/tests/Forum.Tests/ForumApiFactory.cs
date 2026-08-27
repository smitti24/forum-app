using System.Net.Http.Json;
using System.Text.Json;
using Forum.Api.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Tests;

public class ForumApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";

    private readonly SqliteConnection _keepAlive;

    public ForumApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected virtual int CredentialAttemptsPerMinute => 10_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Forum", _connectionString);
        builder.UseSetting("RateLimiting:CredentialAttemptsPerMinute", CredentialAttemptsPerMinute.ToString());
    }

    public async Task<string> RegisterAndLoginAsync(
        HttpClient client,
        string username,
        string password = "a-long-enough-password")
    {
        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"{username}@example.com",
            username,
            password
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = username,
            password
        });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        return body.GetProperty("accessToken").GetString()!;
    }

    public async Task<T> WithDbAsync<T>(Func<ForumDbContext, Task<T>> work)
    {
        using var scope = Services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<ForumDbContext>());
    }

    public async Task WithDbAsync(Func<ForumDbContext, Task> work) =>
        await WithDbAsync<object?>(async db =>
        {
            await work(db);
            return null;
        });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
