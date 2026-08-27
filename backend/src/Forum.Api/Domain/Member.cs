namespace Forum.Api.Domain;

public enum MemberRole
{
    Member,
    Moderator
}

public class Member
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string EmailNormalized { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string UsernameNormalized { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public MemberRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
