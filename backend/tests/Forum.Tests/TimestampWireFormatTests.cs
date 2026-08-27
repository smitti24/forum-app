using System.Net.Http.Json;
using System.Text.Json;

namespace Forum.Tests;

public class TimestampWireFormatTests
{
    [Fact]
    public async Task A_timestamp_is_serialised_as_utc_on_the_wire()
    {
        using var factory = new ForumApiFactory();
        var client = await PostTests.AuthenticatedClientAsync(factory, "asmith");
        var id = await PostTests.CreatePostAsync(client);

        var response = await factory.CreateClient().GetAsync($"/api/v1/posts/{id}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var createdAt = body.GetProperty("createdAt").GetString()!;

        Assert.EndsWith("Z", createdAt);
    }
}
