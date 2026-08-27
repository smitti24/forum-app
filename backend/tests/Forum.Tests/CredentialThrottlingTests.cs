using System.Net;
using System.Net.Http.Json;

namespace Forum.Tests;

public class CredentialThrottlingTests
{
    private sealed class ThrottledFactory : ForumApiFactory
    {
        protected override int CredentialAttemptsPerMinute => 3;
    }

    [Fact]
    public async Task Repeated_credential_attempts_are_throttled()
    {
        using var factory = new ThrottledFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? last = null;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            last = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                identifier = "nobody",
                password = "the-wrong-password-entirely"
            });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }
}
