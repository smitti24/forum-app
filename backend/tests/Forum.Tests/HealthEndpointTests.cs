using System.Net;

namespace Forum.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task The_api_starts_and_reports_healthy()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_schema_is_migrated_on_startup()
    {
        using var factory = new ForumApiFactory();
        factory.CreateClient();

        var members = await factory.WithDbAsync(db => db.Members.CountAsync());

        Assert.Equal(0, members);
    }
}
