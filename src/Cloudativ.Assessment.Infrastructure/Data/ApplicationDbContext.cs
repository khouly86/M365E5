using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Entities.Inventory;
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
    public DbSet<UserDomainAccess> UserDomainAccess => Set<UserDomainAccess>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<GovernanceAnalysis> GovernanceAnalyses => Set<GovernanceAnalysis>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // Inventory DbSets
    public DbSet<InventorySnapshot> InventorySnapshots => Set<InventorySnapshot>();
    public DbSet<TenantInfo> TenantInfos => Set<TenantInfo>();
    public DbSet<LicenseSubscription> LicenseSubscriptions => Set<LicenseSubscription>();
    public DbSet<UserInventory> UserInventories => Set<UserInventory>();
    public DbSet<GroupInventory> GroupInventories => Set<GroupInventory>();
    public DbSet<DirectoryRoleInventory> DirectoryRoleInventories => Set<DirectoryRoleInventory>();
    public DbSet<ConditionalAccessPolicyInventory> ConditionalAccessPolicyInventories => Set<ConditionalAccessPolicyInventory>();
    public DbSet<ServicePrincipalInventory> ServicePrincipalInventories => Set<ServicePrincipalInventory>();
    public DbSet<ManagedIdentityInventory> ManagedIdentityInventories => Set<ManagedIdentityInventory>();
    public DbSet<NamedLocationInventory> NamedLocationInventories => Set<NamedLocationInventory>();
    public DbSet<AuthenticationMethodInventory> AuthenticationMethodInventories => Set<AuthenticationMethodInventory>();
    public DbSet<DeviceInventory> DeviceInventories => Set<DeviceInventory>();
    public DbSet<CompliancePolicyInventory> CompliancePolicyInventories => Set<CompliancePolicyInventory>();
    public DbSet<ConfigurationProfileInventory> ConfigurationProfileInventories => Set<ConfigurationProfileInventory>();
    public DbSet<DefenderForEndpointInventory> DefenderForEndpointInventories => Set<DefenderForEndpointInventory>();
    public DbSet<DefenderForOffice365Inventory> DefenderForOffice365Inventories => Set<DefenderForOffice365Inventory>();
    public DbSet<DefenderForIdentityInventory> DefenderForIdentityInventories => Set<DefenderForIdentityInventory>();
    public DbSet<DefenderForCloudAppsInventory> DefenderForCloudAppsInventories => Set<DefenderForCloudAppsInventory>();
    public DbSet<ExchangeOrganizationInventory> ExchangeOrganizationInventories => Set<ExchangeOrganizationInventory>();
    public DbSet<SensitivityLabelInventory> SensitivityLabelInventories => Set<SensitivityLabelInventory>();
    public DbSet<DlpPolicyInventory> DlpPolicyInventories => Set<DlpPolicyInventory>();
    public DbSet<ComplianceInventory> ComplianceInventories => Set<ComplianceInventory>();
    public DbSet<SharePointSettingsInventory> SharePointSettingsInventories => Set<SharePointSettingsInventory>();
    public DbSet<SharePointSiteInventory> SharePointSiteInventories => Set<SharePointSiteInventory>();
    public DbSet<TeamsInventory> TeamsInventories => Set<TeamsInventory>();
    public DbSet<TeamsSettingsInventory> TeamsSettingsInventories => Set<TeamsSettingsInventory>();
    public DbSet<EnterpriseAppInventory> EnterpriseAppInventories => Set<EnterpriseAppInventory>();
    public DbSet<OAuthConsentInventory> OAuthConsentInventories => Set<OAuthConsentInventory>();
    public DbSet<AuditLogInventory> AuditLogInventories => Set<AuditLogInventory>();
    public DbSet<SecureScoreInventory> SecureScoreInventories => Set<SecureScoreInventory>();
    public DbSet<LicenseUtilizationInventory> LicenseUtilizationInventories => Set<LicenseUtilizationInventory>();
    public DbSet<LicenseCategoryUtilization> LicenseCategoryUtilizations => Set<LicenseCategoryUtilization>();
    public DbSet<HighRiskFindingInventory> HighRiskFindingInventories => Set<HighRiskFindingInventory>();

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

        // UserDomainAccess configuration (for Domain-level admins)
        modelBuilder.Entity<UserDomainAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AppUserId, e.Domain }).IsUnique();

            entity.HasOne(e => e.AppUser)
                .WithMany(u => u.DomainAccess)
                .HasForeignKey(e => e.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Subscription configuration
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.HasIndex(e => e.StripeCustomerId);
            entity.HasIndex(e => e.StripeSubscriptionId);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(256);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(256);
            entity.Property(e => e.StripePriceId).HasMaxLength(256);
            entity.Property(e => e.MonthlyPrice).HasPrecision(18, 2);
            entity.Property(e => e.YearlyPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Tenant)
                .WithOne(t => t.Subscription)
                .HasForeignKey<Subscription>(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GovernanceAnalysis configuration
        modelBuilder.Entity<GovernanceAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.AssessmentRunId);
            entity.HasIndex(e => new { e.TenantId, e.Standard });
            entity.HasIndex(e => new { e.AssessmentRunId, e.Standard }).IsUnique();
            entity.Property(e => e.AiModelUsed).HasMaxLength(64);
            entity.Property(e => e.StandardDocumentVersion).HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.GovernanceAnalyses)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssessmentRun)
                .WithMany()
                .HasForeignKey(e => e.AssessmentRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === INVENTORY ENTITY CONFIGURATIONS ===

        // InventorySnapshot configuration
        modelBuilder.Entity<InventorySnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InitiatedBy).HasMaxLength(256);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Domain });
            entity.HasIndex(e => new { e.TenantId, e.CollectedAt });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TenantInfo configuration
        modelBuilder.Entity<TenantInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AzureTenantId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.PrimaryDomain).HasMaxLength(256);
            entity.Property(e => e.PreferredDataLocation).HasMaxLength(64);
            entity.Property(e => e.DefaultUsageLocation).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LicenseSubscription configuration
        modelBuilder.Entity<LicenseSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SkuId).HasMaxLength(64);
            entity.Property(e => e.SkuPartNumber).HasMaxLength(128);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.AppliesTo).HasMaxLength(64);
            entity.Property(e => e.CapabilityStatus).HasMaxLength(64);
            entity.Property(e => e.TierGroup).HasMaxLength(64);
            entity.Property(e => e.EstimatedMonthlyPricePerUser).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.SkuPartNumber);
            entity.HasIndex(e => new { e.TenantId, e.LicenseCategory });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserInventory configuration
        modelBuilder.Entity<UserInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ObjectId).HasMaxLength(64);
            entity.Property(e => e.UserPrincipalName).HasMaxLength(256);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.Mail).HasMaxLength(256);
            entity.Property(e => e.UserType).HasMaxLength(32);
            entity.Property(e => e.RiskLevel).HasMaxLength(32);
            entity.Property(e => e.RiskState).HasMaxLength(32);
            entity.Property(e => e.Department).HasMaxLength(128);
            entity.Property(e => e.JobTitle).HasMaxLength(128);
            entity.Property(e => e.UsageLocation).HasMaxLength(8);
            entity.Property(e => e.Country).HasMaxLength(64);
            entity.Property(e => e.OfficeLocation).HasMaxLength(256);
            entity.Property(e => e.CompanyName).HasMaxLength(256);
            entity.Property(e => e.OnPremisesDomainName).HasMaxLength(256);
            entity.Property(e => e.OnPremisesSamAccountName).HasMaxLength(128);
            entity.Property(e => e.ManagerId).HasMaxLength(64);
            entity.Property(e => e.ManagerDisplayName).HasMaxLength(256);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.UserPrincipalName);
            entity.HasIndex(e => e.ObjectId);
            entity.HasIndex(e => new { e.TenantId, e.IsPrivileged });
            entity.HasIndex(e => new { e.TenantId, e.RiskLevel });
            entity.HasIndex(e => new { e.TenantId, e.HasE5License });
            entity.HasIndex(e => new { e.TenantId, e.UserType });
            entity.HasIndex(e => new { e.TenantId, e.PrimaryLicenseCategory });
            entity.Property(e => e.PrimaryLicenseTierGroup).HasMaxLength(64);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GroupInventory configuration
        modelBuilder.Entity<GroupInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ObjectId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.Mail).HasMaxLength(256);
            entity.Property(e => e.MailNickname).HasMaxLength(128);
            entity.Property(e => e.GroupType).HasMaxLength(64);
            entity.Property(e => e.Visibility).HasMaxLength(32);
            entity.Property(e => e.Classification).HasMaxLength(128);
            entity.Property(e => e.SensitivityLabel).HasMaxLength(128);
            entity.Property(e => e.TeamId).HasMaxLength(64);
            entity.Property(e => e.OnPremisesSamAccountName).HasMaxLength(128);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.ObjectId);
            entity.HasIndex(e => new { e.TenantId, e.IsSecurityGroup });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DirectoryRoleInventory configuration
        modelBuilder.Entity<DirectoryRoleInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleTemplateId).HasMaxLength(64);
            entity.Property(e => e.RoleId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.RoleTemplateId);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ConditionalAccessPolicyInventory configuration
        modelBuilder.Entity<ConditionalAccessPolicyInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.State).HasMaxLength(64);
            entity.Property(e => e.GrantControlOperator).HasMaxLength(32);
            entity.Property(e => e.SignInFrequencyValue).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.PolicyId);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServicePrincipalInventory configuration
        modelBuilder.Entity<ServicePrincipalInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ObjectId).HasMaxLength(64);
            entity.Property(e => e.AppId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.ServicePrincipalType).HasMaxLength(64);
            entity.Property(e => e.PublisherName).HasMaxLength(256);
            entity.Property(e => e.VerifiedPublisher).HasMaxLength(256);
            entity.Property(e => e.AppOwnerOrganizationId).HasMaxLength(64);
            entity.Property(e => e.SignInAudience).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.AppId);
            entity.HasIndex(e => new { e.TenantId, e.HasHighPrivilegePermissions });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ManagedIdentityInventory configuration
        modelBuilder.Entity<ManagedIdentityInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ObjectId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.ManagedIdentityType).HasMaxLength(64);
            entity.Property(e => e.AppId).HasMaxLength(64);
            entity.Property(e => e.ResourceId).HasMaxLength(512);
            entity.Property(e => e.AssociatedResourceType).HasMaxLength(128);
            entity.Property(e => e.AssociatedResourceName).HasMaxLength(256);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NamedLocationInventory configuration
        modelBuilder.Entity<NamedLocationInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LocationId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.LocationType).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuthenticationMethodInventory configuration
        modelBuilder.Entity<AuthenticationMethodInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SsprScope).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DeviceInventory configuration
        modelBuilder.Entity<DeviceInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).HasMaxLength(64);
            entity.Property(e => e.AzureAdDeviceId).HasMaxLength(64);
            entity.Property(e => e.DeviceName).HasMaxLength(256);
            entity.Property(e => e.OperatingSystem).HasMaxLength(64);
            entity.Property(e => e.OsVersion).HasMaxLength(64);
            entity.Property(e => e.OsBuildNumber).HasMaxLength(64);
            entity.Property(e => e.Manufacturer).HasMaxLength(128);
            entity.Property(e => e.Model).HasMaxLength(128);
            entity.Property(e => e.SerialNumber).HasMaxLength(128);
            entity.Property(e => e.Imei).HasMaxLength(32);
            entity.Property(e => e.OwnerType).HasMaxLength(32);
            entity.Property(e => e.ManagedDeviceOwnerType).HasMaxLength(32);
            entity.Property(e => e.EnrollmentType).HasMaxLength(64);
            entity.Property(e => e.DeviceEnrollmentType).HasMaxLength(64);
            entity.Property(e => e.ManagementAgent).HasMaxLength(64);
            entity.Property(e => e.ManagementState).HasMaxLength(64);
            entity.Property(e => e.ComplianceState).HasMaxLength(32);
            entity.Property(e => e.JailBroken).HasMaxLength(32);
            entity.Property(e => e.DefenderHealthState).HasMaxLength(64);
            entity.Property(e => e.RiskScore).HasMaxLength(32);
            entity.Property(e => e.ExposureLevel).HasMaxLength(32);
            entity.Property(e => e.DeviceThreatLevel).HasMaxLength(32);
            entity.Property(e => e.EncryptionState).HasMaxLength(64);
            entity.Property(e => e.PrimaryUserUpn).HasMaxLength(256);
            entity.Property(e => e.PrimaryUserDisplayName).HasMaxLength(256);
            entity.Property(e => e.PrimaryUserId).HasMaxLength(64);
            entity.Property(e => e.UserDisplayName).HasMaxLength(256);
            entity.Property(e => e.EmailAddress).HasMaxLength(256);
            entity.Property(e => e.TrustType).HasMaxLength(64);
            entity.Property(e => e.DeviceRegistrationState).HasMaxLength(64);
            entity.Property(e => e.ConfigurationManagerClientEnabled).HasMaxLength(32);
            entity.Property(e => e.AutopilotEnrolled).HasMaxLength(32);
            entity.Property(e => e.DeviceCategory).HasMaxLength(128);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => new { e.TenantId, e.ComplianceState });
            entity.HasIndex(e => new { e.TenantId, e.OperatingSystem });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CompliancePolicyInventory configuration
        modelBuilder.Entity<CompliancePolicyInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.Platform).HasMaxLength(64);
            entity.Property(e => e.PolicyType).HasMaxLength(128);
            entity.Property(e => e.MinOsVersion).HasMaxLength(32);
            entity.Property(e => e.MaxOsVersion).HasMaxLength(32);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ConfigurationProfileInventory configuration
        modelBuilder.Entity<ConfigurationProfileInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProfileId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.Platform).HasMaxLength(64);
            entity.Property(e => e.ProfileType).HasMaxLength(128);
            entity.Property(e => e.TemplateId).HasMaxLength(64);
            entity.Property(e => e.TemplateDisplayName).HasMaxLength(256);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DefenderForEndpointInventory configuration
        modelBuilder.Entity<DefenderForEndpointInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DefenderForOffice365Inventory configuration
        modelBuilder.Entity<DefenderForOffice365Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SafeAttachmentsMode).HasMaxLength(64);
            entity.Property(e => e.DmarcPolicy).HasMaxLength(32);
            entity.Property(e => e.DefaultSpamAction).HasMaxLength(64);
            entity.Property(e => e.HighConfidenceSpamAction).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DefenderForIdentityInventory configuration
        modelBuilder.Entity<DefenderForIdentityInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkspaceId).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DefenderForCloudAppsInventory configuration
        modelBuilder.Entity<DefenderForCloudAppsInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExchangeOrganizationInventory configuration
        modelBuilder.Entity<ExchangeOrganizationInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DefaultDomain).HasMaxLength(256);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SensitivityLabelInventory configuration
        modelBuilder.Entity<SensitivityLabelInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LabelId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.ParentLabelId).HasMaxLength(64);
            entity.Property(e => e.EncryptionProtectionType).HasMaxLength(64);
            entity.Property(e => e.Color).HasMaxLength(32);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DlpPolicyInventory configuration
        modelBuilder.Entity<DlpPolicyInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PolicyId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.Mode).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ComplianceInventory configuration
        modelBuilder.Entity<ComplianceInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SharePointSettingsInventory configuration
        modelBuilder.Entity<SharePointSettingsInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SharingCapability).HasMaxLength(64);
            entity.Property(e => e.DefaultSharingLinkType).HasMaxLength(64);
            entity.Property(e => e.DefaultLinkPermission).HasMaxLength(64);
            entity.Property(e => e.UnmanagedDevicePolicy).HasMaxLength(64);
            entity.Property(e => e.OneDriveSharingCapability).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SharePointSiteInventory configuration
        modelBuilder.Entity<SharePointSiteInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SiteId).HasMaxLength(64);
            entity.Property(e => e.WebUrl).HasMaxLength(512);
            entity.Property(e => e.Title).HasMaxLength(256);
            entity.Property(e => e.Template).HasMaxLength(64);
            entity.Property(e => e.WebTemplate).HasMaxLength(64);
            entity.Property(e => e.HubSiteId).HasMaxLength(64);
            entity.Property(e => e.GroupId).HasMaxLength(64);
            entity.Property(e => e.SharingCapability).HasMaxLength(64);
            entity.Property(e => e.SharingDomainRestrictionMode).HasMaxLength(64);
            entity.Property(e => e.OwnerUpn).HasMaxLength(256);
            entity.Property(e => e.OwnerDisplayName).HasMaxLength(256);
            entity.Property(e => e.SecondaryOwnerUpn).HasMaxLength(256);
            entity.Property(e => e.SensitivityLabel).HasMaxLength(128);
            entity.Property(e => e.SensitivityLabelId).HasMaxLength(64);
            entity.Property(e => e.Classification).HasMaxLength(128);
            entity.Property(e => e.LockState).HasMaxLength(32);
            entity.Property(e => e.ConditionalAccessPolicy).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => new { e.TenantId, e.HasExternalSharing });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TeamsInventory configuration
        modelBuilder.Entity<TeamsInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TeamId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.Visibility).HasMaxLength(32);
            entity.Property(e => e.Classification).HasMaxLength(128);
            entity.Property(e => e.GroupId).HasMaxLength(64);
            entity.Property(e => e.MailNickname).HasMaxLength(128);
            entity.Property(e => e.SensitivityLabel).HasMaxLength(128);
            entity.Property(e => e.SensitivityLabelId).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.TeamId);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TeamsSettingsInventory configuration
        modelBuilder.Entity<TeamsSettingsInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DefaultScreenSharingMode).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EnterpriseAppInventory configuration
        modelBuilder.Entity<EnterpriseAppInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ObjectId).HasMaxLength(64);
            entity.Property(e => e.AppId).HasMaxLength(64);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.PublisherName).HasMaxLength(256);
            entity.Property(e => e.VerifiedPublisherName).HasMaxLength(256);
            entity.Property(e => e.AppOwnerOrganizationId).HasMaxLength(64);
            entity.Property(e => e.ServicePrincipalType).HasMaxLength(64);
            entity.Property(e => e.SignInAudience).HasMaxLength(64);
            entity.Property(e => e.DelegatedPermissionConsentType).HasMaxLength(64);
            entity.Property(e => e.Homepage).HasMaxLength(512);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => e.AppId);
            entity.HasIndex(e => new { e.TenantId, e.HasHighPrivilegePermissions });
            entity.HasIndex(e => new { e.TenantId, e.HasExpiredCredentials });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OAuthConsentInventory configuration
        modelBuilder.Entity<OAuthConsentInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserConsentScope).HasMaxLength(64);
            entity.Property(e => e.GroupOwnerConsentScope).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLogInventory configuration
        modelBuilder.Entity<AuditLogInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SecureScoreInventory configuration
        modelBuilder.Entity<SecureScoreInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LicenseUtilizationInventory configuration
        modelBuilder.Entity<LicenseUtilizationInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.E5MonthlyPricePerUser).HasPrecision(18, 2);
            entity.Property(e => e.E3MonthlyPricePerUser).HasPrecision(18, 2);
            entity.Property(e => e.EstimatedMonthlyWaste).HasPrecision(18, 2);
            entity.Property(e => e.EstimatedAnnualWaste).HasPrecision(18, 2);
            entity.Property(e => e.PotentialSavingsIfDowngraded).HasPrecision(18, 2);
            entity.Property(e => e.TotalEstimatedMonthlyWaste).HasPrecision(18, 2);
            entity.Property(e => e.TotalEstimatedAnnualWaste).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LicenseCategoryUtilization configuration
        modelBuilder.Entity<LicenseCategoryUtilization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CategoryDisplayName).HasMaxLength(128);
            entity.Property(e => e.TierGroup).HasMaxLength(64);
            entity.Property(e => e.EstimatedMonthlyPricePerUser).HasPrecision(18, 2);
            entity.Property(e => e.TotalMonthlyLicenseCost).HasPrecision(18, 2);
            entity.Property(e => e.EstimatedMonthlyWaste).HasPrecision(18, 2);
            entity.Property(e => e.EstimatedAnnualWaste).HasPrecision(18, 2);
            entity.Property(e => e.PotentialSavingsIfDowngraded).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => new { e.TenantId, e.LicenseCategory });
            entity.HasIndex(e => new { e.SnapshotId, e.LicenseCategory });

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HighRiskFindingInventory configuration
        modelBuilder.Entity<HighRiskFindingInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FindingType).HasMaxLength(64);
            entity.Property(e => e.FindingCode).HasMaxLength(64);
            entity.Property(e => e.Category).HasMaxLength(64);
            entity.Property(e => e.Severity).HasMaxLength(32);
            entity.Property(e => e.Title).HasMaxLength(256);
            entity.Property(e => e.TrendDirection).HasMaxLength(32);
            entity.Property(e => e.AcknowledgedBy).HasMaxLength(256);
            entity.HasIndex(e => new { e.TenantId, e.SnapshotId });
            entity.HasIndex(e => new { e.TenantId, e.Severity });
            entity.HasIndex(e => e.FindingType);

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.SnapshotId)
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
