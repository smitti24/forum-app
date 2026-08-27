using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Forum.Tests;

public class LoginTests
{
    [Theory]
    [InlineData("asmith")]
    [InlineData("asmith@example.com")]
    [InlineData("ASmith")]
    [InlineData("ASMITH@EXAMPLE.COM")]
    public async Task A_member_can_log_in_with_either_identifier_in_any_case(string identifier)
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await RegisterAsmith(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier,
            password = "a-long-enough-password"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_token_carries_the_member_id_and_role()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await RegisterAsmith(client);
        var token = await factory.RegisterAndLoginAsync(client, "asmith");

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        var id = jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var role = jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value;

        var expected = await factory.WithDbAsync(db => db.Members.Select(m => m.Id).SingleAsync());

        Assert.Equal(expected.ToString(), id);
        Assert.Equal("member", role);
    }

    [Fact]
    public async Task An_unknown_identifier_and_a_wrong_password_fail_identically()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await RegisterAsmith(client);

        var wrongPassword = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = "asmith",
            password = "the-wrong-password-entirely"
        });

        var unknownMember = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = "nobody",
            password = "the-wrong-password-entirely"
        });

        var wrongPasswordBody = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var unknownMemberBody = await unknownMember.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownMember.StatusCode);
        Assert.Equal(
            wrongPasswordBody.GetProperty("detail").GetString(),
            unknownMemberBody.GetProperty("detail").GetString());
        Assert.Equal(
            wrongPasswordBody.GetProperty("title").GetString(),
            unknownMemberBody.GetProperty("title").GetString());
    }

    [Fact]
    public async Task The_profile_endpoint_returns_the_callers_own_email()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var token = await factory.RegisterAndLoginAsync(client, "asmith");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/auth/me");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("asmith@example.com", body.GetProperty("email").GetString());
        Assert.Equal("member", body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task The_profile_endpoint_refuses_an_unauthenticated_caller()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_tampered_token_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var token = await factory.RegisterAndLoginAsync(client, "asmith");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token[..^2] + "xx");

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Task<HttpResponseMessage> RegisterAsmith(HttpClient client) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "asmith@example.com",
            username = "asmith",
            password = "a-long-enough-password"
        });
}
