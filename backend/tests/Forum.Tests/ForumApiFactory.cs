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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Forum", _connectionString);
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
