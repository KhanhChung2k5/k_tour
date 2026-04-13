using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<POI> POIs { get; set; }
    public DbSet<POIContent> POIContents { get; set; }
    public DbSet<VisitLog> VisitLogs { get; set; }
    public DbSet<Analytics> Analytics { get; set; }
    public DbSet<MobileSubscriptionPayment> MobileSubscriptionPayments { get; set; }
    public DbSet<POIPayment> POIPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
        });

        // POI configuration
        modelBuilder.Entity<POI>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // POIContent configuration
        modelBuilder.Entity<POIContent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.POI)
                  .WithMany(p => p.Contents)
                  .HasForeignKey(e => e.POId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // VisitLog configuration
        modelBuilder.Entity<VisitLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.POI)
                  .WithMany()
                  .HasForeignKey(e => e.POId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MobileSubscriptionPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransferRef);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReportedAtUtc);
        });

        modelBuilder.Entity<POIPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransferRef).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.POIId);
            entity.HasOne(e => e.POI)
                  .WithMany()
                  .HasForeignKey(e => e.POIId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
