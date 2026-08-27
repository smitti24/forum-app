using Forum.Api.Domain;

namespace Forum.Tests;

public class IdentityTests
{
    [Theory]
    [InlineData("  ASmith  ", "asmith")]
    [InlineData("ASmith@Example.COM", "asmith@example.com")]
    public void Normalising_trims_and_lowercases(string input, string expected) =>
        Assert.Equal(expected, Identity.Normalise(input));

    [Fact]
    public void A_username_may_not_contain_an_at_sign() =>
        Assert.False(Identity.HasNoAtSign("victim@example.com"));

    [Theory]
    [InlineData("asmith")]
    [InlineData("a.smith")]
    [InlineData("a_smith")]
    [InlineData("a-smith")]
    [InlineData("asmith99")]
    public void A_valid_username_is_accepted(string username) =>
        Assert.True(Identity.IsValidUsername(username));

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    [InlineData("a/b")]
    [InlineData("a?b")]
    [InlineData("a#b")]
    [InlineData("a b")]
    [InlineData("victim@example.com")]
    public void An_invalid_username_is_rejected(string username) =>
        Assert.False(Identity.IsValidUsername(username));

    [Fact]
    public void An_over_long_username_is_rejected() =>
        Assert.False(Identity.IsValidUsername(new string('a', Identity.MaxUsernameLength + 1)));

    [Theory]
    [InlineData("asmith@example.com", true)]
    [InlineData("asmith", false)]
    public void An_identifier_is_classified_as_an_email_only_when_it_contains_an_at_sign(
        string identifier,
        bool isEmail) =>
        Assert.Equal(isEmail, Identity.LooksLikeEmail(identifier));
}
