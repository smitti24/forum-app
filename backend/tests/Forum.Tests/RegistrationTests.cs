using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Forum.Api.Domain;

namespace Forum.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task A_visitor_can_register()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "asmith@example.com",
            username = "asmith",
            password = "a-long-enough-password"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("asmith", body.GetProperty("member").GetProperty("username").GetString());
    }

    [Fact]
    public async Task Registration_signs_the_new_member_in()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await Register(client, "asmith@example.com", "asmith");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("token").GetString()));
        Assert.Equal("member", body.GetProperty("member").GetProperty("role").GetString());
    }

    [Fact]
    public async Task A_new_member_is_never_a_moderator()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "asmith@example.com",
            username = "asmith",
            password = "a-long-enough-password",
            role = "Moderator"
        });

        var role = await factory.WithDbAsync(db => db.Members.Select(m => m.Role).SingleAsync());

        Assert.Equal(MemberRole.Member, role);
    }

    [Fact]
    public async Task A_username_already_taken_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await Register(client, "asmith@example.com", "asmith");
        var second = await Register(client, "different@example.com", "asmith");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task An_email_already_taken_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await Register(client, "asmith@example.com", "asmith");
        var second = await Register(client, "asmith@example.com", "different");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_username_differing_only_by_case_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await Register(client, "asmith@example.com", "asmith");
        var second = await Register(client, "different@example.com", "ASmith");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_conflict_never_reveals_which_identifier_was_taken()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await Register(client, "asmith@example.com", "asmith");

        var emailClash = await Register(client, "asmith@example.com", "different");
        var usernameClash = await Register(client, "different@example.com", "asmith");

        var emailBody = await emailClash.Content.ReadFromJsonAsync<JsonElement>();
        var usernameBody = await usernameClash.Content.ReadFromJsonAsync<JsonElement>();

        var emailDetail = emailBody.GetProperty("detail").GetString()!;
        var usernameDetail = usernameBody.GetProperty("detail").GetString()!;

        Assert.Equal(emailClash.StatusCode, usernameClash.StatusCode);
        Assert.Equal(emailDetail, usernameDetail);
        Assert.DoesNotContain("asmith@example.com", emailDetail);
        Assert.DoesNotContain("asmith", emailDetail);
    }

    [Fact]
    public async Task A_username_containing_an_at_sign_is_told_exactly_that()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await Register(client, "asmith@example.com", "victim@example.com");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("@", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_short_password_is_refused_with_an_explanation()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "asmith@example.com",
            username = "asmith",
            password = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var message = body.GetProperty("errors").GetProperty("password")[0].GetString();

        Assert.Contains("12 characters", message);
    }

    [Fact]
    public async Task A_validation_failure_names_the_field_that_caused_it()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "not-an-email",
            username = "asmith",
            password = "a-long-enough-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Fact]
    public async Task A_password_is_never_stored_in_plaintext()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        await Register(client, "asmith@example.com", "asmith");

        var hash = await factory.WithDbAsync(db => db.Members.Select(m => m.PasswordHash).SingleAsync());

        Assert.DoesNotContain("a-long-enough-password", hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public async Task Registration_never_returns_the_password_or_the_hash()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await Register(client, "asmith@example.com", "asmith");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("a-long-enough-password", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    private static Task<HttpResponseMessage> Register(HttpClient client, string email, string username) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            username,
            password = "a-long-enough-password"
        });
}
