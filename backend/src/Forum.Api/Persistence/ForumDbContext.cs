using Forum.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Persistence;

public class ForumDbContext(DbContextOptions<ForumDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Member>(member =>
        {
            member.HasKey(m => m.Id);
            member.Property(m => m.Email).HasMaxLength(320).IsRequired();
            member.Property(m => m.EmailNormalized).HasMaxLength(320).IsRequired();
            member.Property(m => m.Username).HasMaxLength(32).IsRequired();
            member.Property(m => m.UsernameNormalized).HasMaxLength(32).IsRequired();
            member.Property(m => m.PasswordHash).IsRequired();
            member.Property(m => m.Role).HasConversion<string>().HasMaxLength(16).IsRequired();

            member.HasIndex(m => m.EmailNormalized).IsUnique();
            member.HasIndex(m => m.UsernameNormalized).IsUnique();
        });

        builder.Entity<Post>(post =>
        {
            post.HasKey(p => p.Id);
            post.Property(p => p.Title).HasMaxLength(200).IsRequired();
            post.Property(p => p.Body).HasMaxLength(10_000).IsRequired();
            post.Property(p => p.FlagNote).HasMaxLength(1_000);

            post.HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            post.HasOne(p => p.FlaggedBy)
                .WithMany()
                .HasForeignKey(p => p.FlaggedById)
                .OnDelete(DeleteBehavior.Restrict);

            post.HasIndex(p => p.CreatedAt);
            post.HasIndex(p => p.LikeCount);
            post.HasIndex(p => p.AuthorId);
            post.HasIndex(p => p.IsFlagged);
        });

        builder.Entity<Comment>(comment =>
        {
            comment.HasKey(c => c.Id);
            comment.Property(c => c.Body).HasMaxLength(5_000).IsRequired();

            comment.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            comment.HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            comment.HasIndex(c => new { c.PostId, c.CreatedAt });
        });

        builder.Entity<Like>(like =>
        {
            like.HasKey(l => new { l.PostId, l.MemberId });

            like.HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            like.HasOne(l => l.Member)
                .WithMany()
                .HasForeignKey(l => l.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            like.HasIndex(l => l.MemberId);
        });
    }
}
