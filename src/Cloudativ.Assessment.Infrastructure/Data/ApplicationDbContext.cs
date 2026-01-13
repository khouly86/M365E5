using Cloudativ.Assessment.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cloudativ.Assessment.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<AssessmentRun> AssessmentRuns => Set<AssessmentRun>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<RawSnapshot> RawSnapshots => Set<RawSnapshot>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<TenantUserAccess> TenantUserAccess => Set<TenantUserAccess>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Domain).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.HasIndex(e => e.AzureTenantId);
            entity.Property(e => e.ClientId).HasMaxLength(256);
            entity.Property(e => e.ClientSecretEncrypted).HasMaxLength(1024);
            entity.Property(e => e.Industry).HasMaxLength(128);
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.Notes).HasMaxLength(2048);

            entity.HasOne(e => e.Settings)
                .WithOne(s => s.Tenant)
                .HasForeignKey<TenantSettings>(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TenantSettings configuration
        modelBuilder.Entity<TenantSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ScheduleCron).HasMaxLength(128);
            entity.Property(e => e.NotificationEmails).HasMaxLength(1024);
            entity.Property(e => e.AiModel).HasMaxLength(64);
        });

        // AssessmentRun configuration
        modelBuilder.Entity<AssessmentRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InitiatedBy).HasMaxLength(256);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => new { e.TenantId, e.StartedAt });

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.AssessmentRuns)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Finding configuration
        modelBuilder.Entity<Finding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4096);
            entity.Property(e => e.Category).HasMaxLength(128);
            entity.Property(e => e.CheckId).HasMaxLength(64);
            entity.Property(e => e.CheckName).HasMaxLength(256);
            entity.Property(e => e.Remediation).HasMaxLength(4096);
            entity.Property(e => e.References).HasMaxLength(2048);
            entity.Property(e => e.AffectedResources).HasMaxLength(4096);
            entity.HasIndex(e => e.AssessmentRunId);
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => new { e.AssessmentRunId, e.Domain });

            entity.HasOne(e => e.AssessmentRun)
                .WithMany(r => r.Findings)
                .HasForeignKey(e => e.AssessmentRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RawSnapshot configuration
        modelBuilder.Entity<RawSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataType).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.HasIndex(e => e.AssessmentRunId);
            entity.HasIndex(e => new { e.AssessmentRunId, e.Domain });

            entity.HasOne(e => e.AssessmentRun)
                .WithMany(r => r.RawSnapshots)
                .HasForeignKey(e => e.AssessmentRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AppUser configuration
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.ExternalAuthProvider).HasMaxLength(64);
            entity.Property(e => e.ExternalAuthId).HasMaxLength(256);
            entity.Property(e => e.RefreshToken).HasMaxLength(512);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // TenantUserAccess configuration
        modelBuilder.Entity<TenantUserAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AppUserId, e.TenantId }).IsUnique();

            entity.HasOne(e => e.AppUser)
                .WithMany(u => u.TenantAccess)
                .HasForeignKey(e => e.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
