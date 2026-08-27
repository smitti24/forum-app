using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Forum.Tests;

public class ErrorShapeTests
{
    [Fact]
    public async Task A_malformed_body_is_a_bad_request_that_names_no_internal_type()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", raw);
        Assert.DoesNotContain("Microsoft.", raw);

        var body = JsonSerializer.Deserialize<JsonElement>(raw);
        Assert.Equal("Bad request", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task An_absent_body_is_a_bad_request_that_names_no_internal_type()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("Exception", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_unparseable_date_filter_is_refused()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/posts?from=notadate");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Paging_parameters_below_the_floor_are_clamped_rather_than_refused()
    {
        using var factory = new ForumApiFactory();
        var client = factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/posts?page=-5&pageSize=0");

        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(1, body.GetProperty("pageSize").GetInt32());
    }
}
