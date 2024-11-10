using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class UrlContext : IdentityDbContext<IdentityUser>  // Use IdentityUser class
{
    public DbSet<Url> Urls { get; set; }

    public UrlContext(DbContextOptions<UrlContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensure ShortenedCode is unique
        modelBuilder.Entity<Url>()
            .HasIndex(u => u.ShortenedCode)
            .IsUnique();

        // Set the default value for HitCount
        modelBuilder.Entity<Url>()
            .Property(u => u.HitCount)
            .HasDefaultValue(0);

        // Define the relationship between Url and IdentityUser
        modelBuilder.Entity<Url>()
            .HasOne(u => u.User)
            .WithMany()  // A user can have many URLs
            .HasForeignKey(u => u.UserId);  // The UserId field links the URL to a user
    }
}
