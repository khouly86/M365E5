using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloudativ.Assessment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventorySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Domain = table.Column<int>(type: "INTEGER", nullable: false),
                    InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    WarningsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    ItemsAdded = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemsRemoved = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemsModified = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySnapshots_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnifiedAuditLogEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AdvancedAuditEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdvancedAuditRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    MailboxAuditingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxAuditingByDefaultEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxAuditLogAgeLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    SignInLogsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    SignInLogRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AzureActivityLogsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SharePointAuditingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamsAuditingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExchangeAuditingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSentinelIntegration = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSplunkIntegration = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasOtherSiemIntegration = table.Column<bool>(type: "INTEGER", nullable: false),
                    SiemIntegrationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    DiagnosticSettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    DiagnosticSettingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LogsToStorageAccount = table.Column<bool>(type: "INTEGER", nullable: false),
                    LogsToLogAnalytics = table.Column<bool>(type: "INTEGER", nullable: false),
                    LogsToEventHub = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlertPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EnabledAlertPolicies = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomAlertPolicies = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemAlertPolicies = table.Column<int>(type: "INTEGER", nullable: false),
                    AlertPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    ThreatIntelligenceEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActivityAlertCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityAlertsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityAlertPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ComplianceAlertPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationMethodInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MicrosoftAuthenticatorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Fido2Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    VoiceEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailOtpEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoftwareOathEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TemporaryAccessPassEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertificateBasedAuthEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MicrosoftAuthenticatorTargetJson = table.Column<string>(type: "TEXT", nullable: true),
                    Fido2TargetJson = table.Column<string>(type: "TEXT", nullable: true),
                    SmsTargetJson = table.Column<string>(type: "TEXT", nullable: true),
                    PolicySettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    TotalUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    MfaRegisteredUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    MfaCapableUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    PasswordlessCapableUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthenticatorAppUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    Fido2Users = table.Column<int>(type: "INTEGER", nullable: false),
                    PhoneAuthUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    SoftwareOathUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    PerUserMfaEnabledCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PerUserMfaEnforcedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SsprEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SsprScope = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SsprRegisteredUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    PasswordExpirationDays = table.Column<int>(type: "INTEGER", nullable: false),
                    PasswordNeverExpires = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmartLockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmartLockoutThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    SmartLockoutDurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    BannedPasswordsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnforceCustomBannedPasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationMethodInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthenticationMethodInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthenticationMethodInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InsiderRiskEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    InsiderRiskPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InsiderRiskEnabledPolicies = table.Column<int>(type: "INTEGER", nullable: false),
                    InsiderRiskPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    InsiderRiskOpenAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    InsiderRiskOpenCases = table.Column<int>(type: "INTEGER", nullable: false),
                    CommunicationComplianceEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommunicationCompliancePolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CommunicationCompliancePoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    CommunicationCompliancePendingReview = table.Column<int>(type: "INTEGER", nullable: false),
                    InformationBarriersEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    InformationBarrierPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InformationBarrierSegmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InformationBarrierPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    ActiveEDiscoveryCases = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalEDiscoveryCases = table.Column<int>(type: "INTEGER", nullable: false),
                    EDiscoveryStandardCases = table.Column<int>(type: "INTEGER", nullable: false),
                    EDiscoveryPremiumCases = table.Column<int>(type: "INTEGER", nullable: false),
                    ClosedEDiscoveryCases = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentSearchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveContentSearches = table.Column<int>(type: "INTEGER", nullable: false),
                    RetentionLabelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PublishedRetentionLabels = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoApplyRetentionLabels = table.Column<int>(type: "INTEGER", nullable: false),
                    RetentionLabelsJson = table.Column<string>(type: "TEXT", nullable: true),
                    RetentionPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RetentionPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    RetentionForExchange = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetentionForSharePoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetentionForOneDrive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetentionForTeams = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetentionForYammer = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordsManagementEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    FilePlanCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DispositionReviewEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdvancedAuditEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuditRetentionDays = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplianceInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompliancePolicyInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PolicyType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresEncryption = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresMinOsVersion = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinOsVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    MaxOsVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    RequiresPasswordComplexity = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinPasswordLength = table.Column<int>(type: "INTEGER", nullable: true),
                    BlocksJailbroken = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresDefender = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresStorageEncryption = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresSecureBoot = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresCodeIntegrity = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedDeviceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignmentsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsAssignedToAllDevices = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompliantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NonCompliantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConflictCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NotApplicableCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledActionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    GracePeriodHours = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompliancePolicyInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompliancePolicyInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompliancePolicyInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConditionalAccessPolicyInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IncludeUsersJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExcludeUsersJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludeGroupsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExcludeGroupsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludeRolesJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExcludeRolesJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludesAllUsers = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludesGuestUsers = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcludedUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExcludedGroupCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IncludeApplicationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExcludeApplicationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludesAllApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludesOffice365 = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcludedAppCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientAppTypesJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludesBrowser = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludesMobileApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludesLegacyClients = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlatformsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludesAllPlatforms = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeLocationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExcludeLocationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludesAllLocations = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExcludesTrustedLocations = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeviceStateJson = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceFilterJson = table.Column<string>(type: "TEXT", nullable: true),
                    SignInRiskLevels = table.Column<string>(type: "TEXT", nullable: true),
                    UserRiskLevels = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresMfa = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresCompliantDevice = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresHybridAzureAdJoin = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresApprovedApp = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresAppProtection = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresPasswordChange = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlocksAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlocksLegacyAuth = table.Column<bool>(type: "INTEGER", nullable: false),
                    GrantControlsJson = table.Column<string>(type: "TEXT", nullable: true),
                    GrantControlOperator = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    SessionControlsJson = table.Column<string>(type: "TEXT", nullable: true),
                    HasSignInFrequency = table.Column<bool>(type: "INTEGER", nullable: false),
                    SignInFrequencyValue = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    HasPersistentBrowser = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasCloudAppSecurity = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionalAccessPolicyInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionalAccessPolicyInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConditionalAccessPolicyInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationProfileInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ProfileType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TemplateDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsSecurityBaseline = table.Column<bool>(type: "INTEGER", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SettingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedDeviceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignmentsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsAssignedToAllDevices = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuccessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PendingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConflictCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NotApplicableCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationProfileInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationProfileInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfigurationProfileInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefenderForCloudAppsInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConnectedAppCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectedAppsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Office365Connected = table.Column<bool>(type: "INTEGER", nullable: false),
                    AzureConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    AwsConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    GcpConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    OAuthAppCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HighRiskOAuthApps = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumRiskOAuthApps = table.Column<int>(type: "INTEGER", nullable: false),
                    LowRiskOAuthApps = table.Column<int>(type: "INTEGER", nullable: false),
                    OAuthAppsJson = table.Column<string>(type: "TEXT", nullable: true),
                    AppGovernanceEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppGovernancePoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AppGovernancePolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AppGovernanceAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AnomalyPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    EnabledPolicies = table.Column<int>(type: "INTEGER", nullable: false),
                    DisabledPolicies = table.Column<int>(type: "INTEGER", nullable: false),
                    CloudDiscoveryEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiscoveredAppCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SanctionedApps = table.Column<int>(type: "INTEGER", nullable: false),
                    UnsanctionedApps = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitoredApps = table.Column<int>(type: "INTEGER", nullable: false),
                    TopDiscoveredAppsJson = table.Column<string>(type: "TEXT", nullable: true),
                    OpenAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    HighSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    LowSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionControlEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SessionControlledApps = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenderForCloudAppsInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefenderForCloudAppsInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DefenderForCloudAppsInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefenderForEndpointInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OnboardedDeviceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalManagedDeviceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OnboardingCoverage = table.Column<double>(type: "REAL", nullable: false),
                    WindowsOnboarded = table.Column<int>(type: "INTEGER", nullable: false),
                    MacOsOnboarded = table.Column<int>(type: "INTEGER", nullable: false),
                    LinuxOnboarded = table.Column<int>(type: "INTEGER", nullable: false),
                    MobileOnboarded = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveSensors = table.Column<int>(type: "INTEGER", nullable: false),
                    InactiveSensors = table.Column<int>(type: "INTEGER", nullable: false),
                    MisconfiguredSensors = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpairedCommunication = table.Column<int>(type: "INTEGER", nullable: false),
                    NoSensorData = table.Column<int>(type: "INTEGER", nullable: false),
                    HighRiskDevices = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumRiskDevices = table.Column<int>(type: "INTEGER", nullable: false),
                    LowRiskDevices = table.Column<int>(type: "INTEGER", nullable: false),
                    NoRiskInfoDevices = table.Column<int>(type: "INTEGER", nullable: false),
                    TamperProtectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    EdrInBlockMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    NetworkProtectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WebProtectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CloudProtectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PuaProtectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RealTimeProtectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AsrRulesConfigured = table.Column<bool>(type: "INTEGER", nullable: false),
                    AsrRulesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AsrRulesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AsrRulesBlockMode = table.Column<int>(type: "INTEGER", nullable: false),
                    AsrRulesAuditMode = table.Column<int>(type: "INTEGER", nullable: false),
                    ExposureScore = table.Column<double>(type: "REAL", nullable: true),
                    SecureScore = table.Column<double>(type: "REAL", nullable: true),
                    VulnerabilityCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CriticalVulnerabilities = table.Column<int>(type: "INTEGER", nullable: false),
                    HighVulnerabilities = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumVulnerabilities = table.Column<int>(type: "INTEGER", nullable: false),
                    MissingPatches = table.Column<int>(type: "INTEGER", nullable: false),
                    MissingKbCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TopVulnerabilitiesJson = table.Column<string>(type: "TEXT", nullable: true),
                    ActiveAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    HighSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    LowSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    InformationalAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenderForEndpointInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefenderForEndpointInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DefenderForEndpointInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefenderForIdentityInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsConfigured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLicensed = table.Column<bool>(type: "INTEGER", nullable: false),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SensorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HealthySensors = table.Column<int>(type: "INTEGER", nullable: false),
                    UnhealthySensors = table.Column<int>(type: "INTEGER", nullable: false),
                    OfflineSensors = table.Column<int>(type: "INTEGER", nullable: false),
                    SensorsJson = table.Column<string>(type: "TEXT", nullable: true),
                    DomainControllersCovered = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDomainControllers = table.Column<int>(type: "INTEGER", nullable: false),
                    CoveragePercentage = table.Column<double>(type: "REAL", nullable: false),
                    OpenHealthIssues = table.Column<int>(type: "INTEGER", nullable: false),
                    HighSeverityHealthIssues = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumSeverityHealthIssues = table.Column<int>(type: "INTEGER", nullable: false),
                    LowSeverityHealthIssues = table.Column<int>(type: "INTEGER", nullable: false),
                    HealthIssuesJson = table.Column<string>(type: "TEXT", nullable: true),
                    HighSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    LowSeverityAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    Last30DaysAlerts = table.Column<int>(type: "INTEGER", nullable: false),
                    HoneytokenAccountsConfigured = table.Column<bool>(type: "INTEGER", nullable: false),
                    HoneytokenAccountCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SensitiveGroupsConfigured = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenderForIdentityInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefenderForIdentityInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DefenderForIdentityInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefenderForOffice365Inventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SafeLinksEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SafeLinksPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    SafeLinksPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SafeLinksForOfficeApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    SafeLinksTrackUserClicks = table.Column<bool>(type: "INTEGER", nullable: false),
                    SafeAttachmentsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SafeAttachmentsPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    SafeAttachmentsPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SafeAttachmentsMode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SafeAttachmentsForSharePoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    AntiPhishPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AntiPhishPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpersonationProtectionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MailboxIntelligenceEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpoofIntelligenceEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProtectedUsersCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ProtectedDomainsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AntiSpamPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AntiSpamPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultSpamAction = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    HighConfidenceSpamAction = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    AntiMalwarePoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AntiMalwarePolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CommonAttachmentTypesFilter = table.Column<bool>(type: "INTEGER", nullable: false),
                    ZeroHourAutoPurgeEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DkimEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DkimEnabledDomains = table.Column<int>(type: "INTEGER", nullable: false),
                    DkimStatusJson = table.Column<string>(type: "TEXT", nullable: true),
                    DmarcEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DmarcPolicy = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DmarcStatusJson = table.Column<string>(type: "TEXT", nullable: true),
                    SpfConfigured = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpfStatusJson = table.Column<string>(type: "TEXT", nullable: true),
                    EmailAuthStatusJson = table.Column<string>(type: "TEXT", nullable: true),
                    Last30DaysMalwareCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Last30DaysPhishCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Last30DaysSpamCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Last30DaysBlockedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenderForOffice365Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefenderForOffice365Inventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DefenderForOffice365Inventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AzureAdDeviceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    OperatingSystem = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OsVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    OsBuildNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Imei = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    OwnerType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ManagedDeviceOwnerType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    EnrollmentType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DeviceEnrollmentType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    EnrolledDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ManagementAgent = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ManagementState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ComplianceState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ComplianceGracePeriodExpirationDateTime = table.Column<string>(type: "TEXT", nullable: true),
                    IsManaged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSupervised = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsJailBroken = table.Column<bool>(type: "INTEGER", nullable: false),
                    JailBroken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    HasDefenderForEndpoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefenderHealthState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RiskScore = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ExposureLevel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DeviceThreatLevel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    EncryptionReportedState = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecoveryKeyEscrowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    EncryptionState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    PrimaryUserUpn = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PrimaryUserDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PrimaryUserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsAzureAdRegistered = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAzureAdJoined = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHybridAzureAdJoined = table.Column<bool>(type: "INTEGER", nullable: false),
                    TrustType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DeviceRegistrationState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ConfigurationManagerClientEnabled = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    AutopilotEnrolled = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    RequireUserEnrollmentApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalStorageSpaceInBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    FreeStorageSpaceInBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    DeviceCategory = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryRoleInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoleTemplateId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsBuiltIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrivileged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsGlobalAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UserMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ServicePrincipalMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MembersJson = table.Column<string>(type: "TEXT", nullable: true),
                    UserMembersJson = table.Column<string>(type: "TEXT", nullable: true),
                    ServicePrincipalMembersJson = table.Column<string>(type: "TEXT", nullable: true),
                    EligibleMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasPimConfiguration = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryRoleInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectoryRoleInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DirectoryRoleInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DlpPolicyInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WorkloadsJson = table.Column<string>(type: "TEXT", nullable: true),
                    AppliesToExchange = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToSharePoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToOneDrive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToTeams = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToEndpoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToPowerBI = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToThirdPartyApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    RuleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EnabledRuleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RulesJson = table.Column<string>(type: "TEXT", nullable: true),
                    SensitiveInfoTypesJson = table.Column<string>(type: "TEXT", nullable: true),
                    SensitiveInfoTypeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UsesTrainableClassifiers = table.Column<bool>(type: "INTEGER", nullable: false),
                    TrainableClassifiersJson = table.Column<string>(type: "TEXT", nullable: true),
                    TrainableClassifierCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UsesExactDataMatch = table.Column<bool>(type: "INTEGER", nullable: false),
                    EdmSchemaNames = table.Column<string>(type: "TEXT", nullable: true),
                    BlocksContent = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotifiesUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratesAlert = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratesIncidentReport = table.Column<bool>(type: "INTEGER", nullable: false),
                    EncryptsContent = table.Column<bool>(type: "INTEGER", nullable: false),
                    Last30DaysMatches = table.Column<int>(type: "INTEGER", nullable: true),
                    Last30DaysIncidents = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DlpPolicyInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DlpPolicyInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DlpPolicyInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseAppInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PublisherName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AccountEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsMicrosoftApp = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVerifiedPublisher = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedPublisherName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AppOwnerOrganizationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ServicePrincipalType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SignInAudience = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserAssignmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupAssignmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSignInDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Last30DaysSignIns = table.Column<int>(type: "INTEGER", nullable: false),
                    DelegatedPermissionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    DelegatedPermissionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DelegatedPermissionConsentType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ApplicationPermissionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ApplicationPermissionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasHighPrivilegePermissions = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMailReadWrite = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMailSend = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasDirectoryReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasFilesReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasUserReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasGroupReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasRoleManagementReadWriteDirectory = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasApplicationReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HighRiskPermissionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordCredentialCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NextPasswordExpiration = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HasExpiredPasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasExpiringPasswords = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordCredentialsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateCredentialCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NextCertificateExpiration = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HasExpiredCertificates = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasExpiringCertificates = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertificateCredentialsJson = table.Column<string>(type: "TEXT", nullable: true),
                    NextCredentialExpiration = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HasExpiredCredentials = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasExpiringCredentials = table.Column<bool>(type: "INTEGER", nullable: false),
                    DaysUntilCredentialExpiration = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnersJson = table.Column<string>(type: "TEXT", nullable: true),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Homepage = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IsAppRoleAssignmentRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseAppInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnterpriseAppInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnterpriseAppInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeOrganizationInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InboundConnectorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OutboundConnectorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectorsJson = table.Column<string>(type: "TEXT", nullable: true),
                    TransportRuleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EnabledTransportRules = table.Column<int>(type: "INTEGER", nullable: false),
                    TransportRulesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AcceptedDomainCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AcceptedDomainsJson = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultDomain = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UsersWithExternalForwarding = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalForwardingUsersJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalForwardingBlocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemoteDomainsJson = table.Column<string>(type: "TEXT", nullable: true),
                    UserMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SharedMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ResourceMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RoomMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InactiveMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ArchivedMailboxCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DistributionGroupCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SecurityGroupMailEnabledCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DynamicDistributionGroupCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ModernAuthEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    BasicAuthEnabledForPop = table.Column<bool>(type: "INTEGER", nullable: false),
                    BasicAuthEnabledForImap = table.Column<bool>(type: "INTEGER", nullable: false),
                    BasicAuthEnabledForSmtp = table.Column<bool>(type: "INTEGER", nullable: false),
                    BasicAuthEnabledForEws = table.Column<bool>(type: "INTEGER", nullable: false),
                    BasicAuthEnabledForOutlook = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuthPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    RetentionPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RetentionTagCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LitigationHoldCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InPlaceHoldCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MailboxAuditingDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    JournalRuleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    EwsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PopEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImapEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MobileDeviceAccessEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultPublicFolderMailbox = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeOrganizationInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeOrganizationInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExchangeOrganizationInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Mail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    MailNickname = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    GroupType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsSecurityGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMicrosoft365Group = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMailEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDistributionList = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnifiedGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDynamicMembership = table.Column<bool>(type: "INTEGER", nullable: false),
                    MembershipRule = table.Column<string>(type: "TEXT", nullable: true),
                    MembershipRuleProcessingState = table.Column<string>(type: "TEXT", nullable: true),
                    MemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasExternalMembers = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExternalMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRoleAssignable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    IsAssignableToRole = table.Column<bool>(type: "INTEGER", nullable: false),
                    Classification = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SensitivityLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    HasTeam = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RenewedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpirationDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OnPremisesSyncEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnPremisesSamAccountName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HighRiskFindingInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FindingType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FindingCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SeverityOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ImpactDescription = table.Column<string>(type: "TEXT", nullable: true),
                    RiskScore = table.Column<double>(type: "REAL", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    AffectedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AffectedResourcesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AffectedResourcesSample = table.Column<string>(type: "TEXT", nullable: true),
                    Remediation = table.Column<string>(type: "TEXT", nullable: true),
                    RemediationSteps = table.Column<string>(type: "TEXT", nullable: true),
                    RemediationUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ComplianceReferences = table.Column<string>(type: "TEXT", nullable: true),
                    DetectionQuery = table.Column<string>(type: "TEXT", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsNew = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstDetectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DaysOpen = table.Column<int>(type: "INTEGER", nullable: true),
                    PreviousAffectedCount = table.Column<int>(type: "INTEGER", nullable: true),
                    TrendDirection = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcknowledgedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcknowledgementNote = table.Column<string>(type: "TEXT", nullable: true),
                    IsExcluded = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExclusionReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HighRiskFindingInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HighRiskFindingInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HighRiskFindingInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicenseSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SkuId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SkuPartNumber = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PrepaidUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsumedUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    SuspendedUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    WarningUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    IsTrial = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AppliesTo = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ServicePlansJson = table.Column<string>(type: "TEXT", nullable: true),
                    CapabilityStatus = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicenseSubscriptions_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LicenseSubscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicenseUtilizationInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    E5LicensesTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    E5LicensesAssigned = table.Column<int>(type: "INTEGER", nullable: false),
                    E5LicensesAvailable = table.Column<int>(type: "INTEGER", nullable: false),
                    E5LicensesSuspended = table.Column<int>(type: "INTEGER", nullable: false),
                    E3LicensesTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    E3LicensesAssigned = table.Column<int>(type: "INTEGER", nullable: false),
                    E3LicensesAvailable = table.Column<int>(type: "INTEGER", nullable: false),
                    F1LicensesTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    F3LicensesTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    AllLicenseSummaryJson = table.Column<string>(type: "TEXT", nullable: true),
                    UsersWithE5NoMfa = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoConditionalAccess = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoPimCoverage = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoIdentityProtection = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminsWithoutPim = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5DefenderNotOnboarded = table.Column<int>(type: "INTEGER", nullable: false),
                    DevicesWithE5NoDefender = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoDefenderForOffice = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoCloudAppSecurity = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoPurviewLabels = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoDlp = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoRetention = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoEDiscovery = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoInsiderRisk = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoTeamsPhoneSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoAudioConferencing = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithE5NoPowerBI = table.Column<int>(type: "INTEGER", nullable: false),
                    NoMfaUsersJson = table.Column<string>(type: "TEXT", nullable: true),
                    NoCaUsersJson = table.Column<string>(type: "TEXT", nullable: true),
                    DefenderNotOnboardedJson = table.Column<string>(type: "TEXT", nullable: true),
                    NoPurviewUsersJson = table.Column<string>(type: "TEXT", nullable: true),
                    NoPimUsersJson = table.Column<string>(type: "TEXT", nullable: true),
                    NoDefenderForOfficeJson = table.Column<string>(type: "TEXT", nullable: true),
                    E5UtilizationPercentage = table.Column<double>(type: "REAL", nullable: false),
                    IdentityFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    SecurityFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    ComplianceFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    OverallFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    E5MonthlyPricePerUser = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    E3MonthlyPricePerUser = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EstimatedMonthlyWaste = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EstimatedAnnualWaste = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PotentialSavingsIfDowngraded = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UsersEligibleForDowngrade = table.Column<int>(type: "INTEGER", nullable: false),
                    RecommendationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CriticalRecommendations = table.Column<int>(type: "INTEGER", nullable: false),
                    HighRecommendations = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumRecommendations = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseUtilizationInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicenseUtilizationInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LicenseUtilizationInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManagedIdentityInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ManagedIdentityType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    AlternativeNames = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedPermissionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PermissionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AssociatedResourceType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    AssociatedResourceName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedIdentityInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedIdentityInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManagedIdentityInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamedLocationInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LocationType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsTrusted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IpRangesJson = table.Column<string>(type: "TEXT", nullable: true),
                    IpRangeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CountriesAndRegionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IncludeUnknownCountriesAndRegions = table.Column<bool>(type: "INTEGER", nullable: false),
                    CountryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamedLocationInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamedLocationInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamedLocationInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OAuthConsentInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserConsentEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserConsentScope = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UserConsentDescription = table.Column<string>(type: "TEXT", nullable: true),
                    AllowUserConsentForRiskyApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockUserConsentForRiskyApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminConsentWorkflowEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminConsentReviewersJson = table.Column<string>(type: "TEXT", nullable: true),
                    AdminConsentReviewerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestExpirationDays = table.Column<int>(type: "INTEGER", nullable: false),
                    NotifyReviewers = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemindersEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PendingAdminConsentRequests = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovedAdminConsentRequests = table.Column<int>(type: "INTEGER", nullable: false),
                    DeniedAdminConsentRequests = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiredAdminConsentRequests = table.Column<int>(type: "INTEGER", nullable: false),
                    PendingRequestsJson = table.Column<string>(type: "TEXT", nullable: true),
                    RiskyConsentBlocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedPublisherRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupOwnerConsentEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupOwnerConsentScope = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    PermissionGrantPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    PermissionGrantPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalOAuthGrantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminGrantedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UserGrantedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HighRiskGrantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthConsentInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthConsentInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OAuthConsentInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecureScoreInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentScore = table.Column<double>(type: "REAL", nullable: false),
                    MaxScore = table.Column<double>(type: "REAL", nullable: false),
                    ScorePercentage = table.Column<double>(type: "REAL", nullable: false),
                    PreviousScore = table.Column<double>(type: "REAL", nullable: true),
                    ScoreChange = table.Column<double>(type: "REAL", nullable: true),
                    ScoreDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IdentityScore = table.Column<double>(type: "REAL", nullable: false),
                    IdentityMaxScore = table.Column<double>(type: "REAL", nullable: false),
                    DeviceScore = table.Column<double>(type: "REAL", nullable: false),
                    DeviceMaxScore = table.Column<double>(type: "REAL", nullable: false),
                    AppsScore = table.Column<double>(type: "REAL", nullable: false),
                    AppsMaxScore = table.Column<double>(type: "REAL", nullable: false),
                    DataScore = table.Column<double>(type: "REAL", nullable: false),
                    DataMaxScore = table.Column<double>(type: "REAL", nullable: false),
                    InfrastructureScore = table.Column<double>(type: "REAL", nullable: false),
                    InfrastructureMaxScore = table.Column<double>(type: "REAL", nullable: false),
                    TotalActions = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedActions = table.Column<int>(type: "INTEGER", nullable: false),
                    NotApplicableActions = table.Column<int>(type: "INTEGER", nullable: false),
                    ToAddressActions = table.Column<int>(type: "INTEGER", nullable: false),
                    InProgressActions = table.Column<int>(type: "INTEGER", nullable: false),
                    PlannedActions = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskAcceptedActions = table.Column<int>(type: "INTEGER", nullable: false),
                    ThirdPartyActions = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolvedThroughAlternate = table.Column<int>(type: "INTEGER", nullable: false),
                    TopImprovementActionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PotentialScoreIncrease = table.Column<double>(type: "REAL", nullable: false),
                    MicrosoftScore = table.Column<double>(type: "REAL", nullable: false),
                    ThirdPartyScore = table.Column<double>(type: "REAL", nullable: true),
                    DefenderExposureScore = table.Column<double>(type: "REAL", nullable: true),
                    DefenderSecureScore = table.Column<double>(type: "REAL", nullable: true),
                    ComplianceAssessmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallComplianceScore = table.Column<double>(type: "REAL", nullable: true),
                    ComplianceAssessmentsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ScoreTrendJson = table.Column<string>(type: "TEXT", nullable: true),
                    Score7DaysAgo = table.Column<double>(type: "REAL", nullable: true),
                    Score30DaysAgo = table.Column<double>(type: "REAL", nullable: true),
                    Score90DaysAgo = table.Column<double>(type: "REAL", nullable: true),
                    E5FeatureScore = table.Column<double>(type: "REAL", nullable: true),
                    E3FeatureScore = table.Column<double>(type: "REAL", nullable: true),
                    E5FeaturesNotUsedJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecureScoreInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecureScoreInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecureScoreInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SensitivityLabelInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LabelId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Tooltip = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentLabelId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    AppliesToFiles = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToEmails = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToSites = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToGroups = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToMeetings = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliesToSchematizedData = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasEncryption = table.Column<bool>(type: "INTEGER", nullable: false),
                    EncryptionSettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    EncryptionProtectionType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    HasContentMarking = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasHeader = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasFooter = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasWatermark = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContentMarkingJson = table.Column<string>(type: "TEXT", nullable: true),
                    AutoLabelingEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoLabelingConditionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    AutoLabelingConditionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasSublabels = table.Column<bool>(type: "INTEGER", nullable: false),
                    SublabelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SublabelsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitivityLabelInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensitivityLabelInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SensitivityLabelInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrincipalInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ServicePrincipalType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AccountEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PublisherName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    VerifiedPublisher = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsMicrosoftFirstParty = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AppOwnerOrganizationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SignInAudience = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastSignInDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DelegatedPermissionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ApplicationPermissionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    DelegatedPermissionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ApplicationPermissionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasHighPrivilegePermissions = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMailReadWrite = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasDirectoryReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasFilesReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasUserReadWriteAll = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasRoleManagementReadWriteDirectory = table.Column<bool>(type: "INTEGER", nullable: false),
                    HighPrivilegePermissionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnersJson = table.Column<string>(type: "TEXT", nullable: true),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsAppRoleAssignmentRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrincipalInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePrincipalInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServicePrincipalInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharePointSettingsInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SharingCapability = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DefaultSharingLinkType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DefaultLinkPermission = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DefaultLinkExpirationDays = table.Column<int>(type: "INTEGER", nullable: true),
                    RequireAcceptingAccountMatchInvitedAccount = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireAnonymousLinksExpireInDays = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnonymousLinkExpirationDays = table.Column<int>(type: "INTEGER", nullable: true),
                    FileAnonymousLinkType = table.Column<bool>(type: "INTEGER", nullable: false),
                    FolderAnonymousLinkType = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowGuestUserSignIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowedDomainList = table.Column<string>(type: "TEXT", nullable: true),
                    BlockedDomainList = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalUserExpirationRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExternalUserExpirationDays = table.Column<int>(type: "INTEGER", nullable: true),
                    ShowEveryoneClaim = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowEveryoneExceptExternalUsersClaim = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConditionalAccessPolicyEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UnmanagedDevicePolicy = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    BlockDownloadOfViewableFilesOnUnmanagedDevices = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockDownloadOfAllFilesOnUnmanagedDevices = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowEditing = table.Column<bool>(type: "INTEGER", nullable: false),
                    OneDriveSharingCapability = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OneDriveStorageQuota = table.Column<long>(type: "INTEGER", nullable: true),
                    OneDriveForGuestsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalSiteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CommunicationSiteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamSiteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassicSiteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SitesWithExternalSharing = table.Column<int>(type: "INTEGER", nullable: false),
                    InactiveSiteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalStorageUsedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalStorageQuotaBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CommentsOnSitePagesDisabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisallowInfectedFileDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExternalServicesEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LegacyAuthProtocolsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationsInOneDriveForBusinessEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationsInSharePointEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MajorVersionLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableAutoExpirationVersionTrim = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharePointSettingsInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharePointSettingsInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharePointSettingsInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharePointSiteInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WebUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Template = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WebTemplate = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastActivityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsInactive = table.Column<bool>(type: "INTEGER", nullable: false),
                    InactiveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    IsTeamSite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCommunicationSite = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHubSite = table.Column<bool>(type: "INTEGER", nullable: false),
                    HubSiteId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsGroupConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SharingCapability = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HasExternalSharing = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowsAnonymousSharing = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExternalUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AnonymousLinkCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SharingDomainRestrictionMode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    StorageUsedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StorageQuotaBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePercentUsed = table.Column<double>(type: "REAL", nullable: false),
                    StorageWarningLevelBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    OwnerUpn = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OwnerDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SecondaryOwnerUpn = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsOrphaned = table.Column<bool>(type: "INTEGER", nullable: false),
                    OwnerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SensitivityLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SensitivityLabelId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Classification = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IsReadOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DenyAddAndCustomizePages = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConditionalAccessPolicy = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FileCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ListCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SubsiteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharePointSiteInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharePointSiteInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharePointSiteInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamsInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Classification = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    GroupId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    MailNickname = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    MemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    GuestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HasExternalMembers = table.Column<bool>(type: "INTEGER", nullable: false),
                    OwnersJson = table.Column<string>(type: "TEXT", nullable: true),
                    TotalChannelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StandardChannelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PrivateChannelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SharedChannelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelsJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InactiveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Last30DaysActiveUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    Last30DaysMessages = table.Column<int>(type: "INTEGER", nullable: false),
                    Last30DaysMeetings = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowCreateUpdateChannels = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowDeleteChannels = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowAddRemoveApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowCreateUpdateRemoveTabs = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowCreateUpdateRemoveConnectors = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowGuestCreateUpdateChannels = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowGuestDeleteChannels = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowGiphy = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowStickersAndMemes = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowCustomMemes = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowUserEditMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowUserDeleteMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowOwnerDeleteMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowTeamMentions = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowChannelMentions = table.Column<bool>(type: "INTEGER", nullable: false),
                    SensitivityLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SensitivityLabelId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    InstalledAppCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TabCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamsInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamsInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamsInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamsSettingsInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AllowFederatedUsers = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowTeamsConsumer = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowTeamsConsumerInbound = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowSkypeBusinessInterop = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowPublicUsers = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowedDomainsJson = table.Column<string>(type: "TEXT", nullable: true),
                    BlockedDomainsJson = table.Column<string>(type: "TEXT", nullable: true),
                    AllowGuestAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    GuestCanCreateChannels = table.Column<bool>(type: "INTEGER", nullable: false),
                    GuestCanDeleteChannels = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowGuestUserToAccessMeetings = table.Column<bool>(type: "INTEGER", nullable: false),
                    MeetingPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    MeetingPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultAllowAnonymousJoin = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowRecording = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowTranscription = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowCloudRecording = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowIPVideo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultScreenSharingMode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DefaultAllowPrivateMeetNow = table.Column<bool>(type: "INTEGER", nullable: false),
                    MessagingPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    MessagingPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultAllowOwnerDeleteMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowUserEditMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowUserDeleteMessage = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowUrlPreviews = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowThirdPartyApps = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowSideloading = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultCatalogAppsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExternalCatalogAppsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomAppsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockedAppsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockedAppsJson = table.Column<string>(type: "TEXT", nullable: true),
                    AppPermissionPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AppPermissionPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AppSetupPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    AppSetupPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CallingPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    CallingPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultAllowPrivateCalling = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowVoicemail = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowCallForwarding = table.Column<bool>(type: "INTEGER", nullable: false),
                    LiveEventsPoliciesJson = table.Column<string>(type: "TEXT", nullable: true),
                    LiveEventsPolicyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultAllowBroadcastScheduling = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAllowBroadcastTranscription = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalTeamsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveTeamsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ArchivedTeamsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamsWithGuests = table.Column<int>(type: "INTEGER", nullable: false),
                    PrivateChannelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SharedChannelCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamsSettingsInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamsSettingsInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamsSettingsInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AzureTenantId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TechnicalNotificationMails = table.Column<string>(type: "TEXT", nullable: true),
                    VerifiedDomainsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryDomain = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    VerifiedDomainCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PreferredDataLocation = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DefaultUsageLocation = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsMultiGeoEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MultiGeoLocationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ModernAuthEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmtpAuthEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LegacyProtocolsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OrganizationSettingsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceHealthJson = table.Column<string>(type: "TEXT", nullable: true),
                    MessageCenterHighlightsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ActiveServiceIssues = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageCenterItemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInfos_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantInfos_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjectId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UserPrincipalName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Mail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UserType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AccountEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSignInDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastNonInteractiveSignInDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsSignInBlocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMfaRegistered = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMfaCapable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPasswordlessCapable = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuthMethodsJson = table.Column<string>(type: "TEXT", nullable: true),
                    HasPerUserMfa = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsSsprRegistered = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSsprEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedLicensesJson = table.Column<string>(type: "TEXT", nullable: true),
                    HasE5License = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasE3License = table.Column<bool>(type: "INTEGER", nullable: false),
                    LicenseCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskLevel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    RiskState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    RiskLastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RiskDetail = table.Column<string>(type: "TEXT", nullable: true),
                    IsPrivileged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsGlobalAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedRolesJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsBreakGlassAccount = table.Column<bool>(type: "INTEGER", nullable: false),
                    DirectRoleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    UsageLocation = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    OfficeLocation = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OnPremisesSyncEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnPremisesLastSyncDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OnPremisesDomainName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OnPremisesSamAccountName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ManagerId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ManagerDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInventories_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInventories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogInventories_SnapshotId",
                table: "AuditLogInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogInventories_TenantId_SnapshotId",
                table: "AuditLogInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationMethodInventories_SnapshotId",
                table: "AuthenticationMethodInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationMethodInventories_TenantId_SnapshotId",
                table: "AuthenticationMethodInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceInventories_SnapshotId",
                table: "ComplianceInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceInventories_TenantId_SnapshotId",
                table: "ComplianceInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePolicyInventories_SnapshotId",
                table: "CompliancePolicyInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePolicyInventories_TenantId_SnapshotId",
                table: "CompliancePolicyInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConditionalAccessPolicyInventories_PolicyId",
                table: "ConditionalAccessPolicyInventories",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionalAccessPolicyInventories_SnapshotId",
                table: "ConditionalAccessPolicyInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionalAccessPolicyInventories_TenantId_SnapshotId",
                table: "ConditionalAccessPolicyInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationProfileInventories_SnapshotId",
                table: "ConfigurationProfileInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationProfileInventories_TenantId_SnapshotId",
                table: "ConfigurationProfileInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForCloudAppsInventories_SnapshotId",
                table: "DefenderForCloudAppsInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForCloudAppsInventories_TenantId_SnapshotId",
                table: "DefenderForCloudAppsInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForEndpointInventories_SnapshotId",
                table: "DefenderForEndpointInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForEndpointInventories_TenantId_SnapshotId",
                table: "DefenderForEndpointInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForIdentityInventories_SnapshotId",
                table: "DefenderForIdentityInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForIdentityInventories_TenantId_SnapshotId",
                table: "DefenderForIdentityInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForOffice365Inventories_SnapshotId",
                table: "DefenderForOffice365Inventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenderForOffice365Inventories_TenantId_SnapshotId",
                table: "DefenderForOffice365Inventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventories_DeviceId",
                table: "DeviceInventories",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventories_SnapshotId",
                table: "DeviceInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventories_TenantId_ComplianceState",
                table: "DeviceInventories",
                columns: new[] { "TenantId", "ComplianceState" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventories_TenantId_OperatingSystem",
                table: "DeviceInventories",
                columns: new[] { "TenantId", "OperatingSystem" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventories_TenantId_SnapshotId",
                table: "DeviceInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryRoleInventories_RoleTemplateId",
                table: "DirectoryRoleInventories",
                column: "RoleTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryRoleInventories_SnapshotId",
                table: "DirectoryRoleInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryRoleInventories_TenantId_SnapshotId",
                table: "DirectoryRoleInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DlpPolicyInventories_SnapshotId",
                table: "DlpPolicyInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DlpPolicyInventories_TenantId_SnapshotId",
                table: "DlpPolicyInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseAppInventories_AppId",
                table: "EnterpriseAppInventories",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseAppInventories_SnapshotId",
                table: "EnterpriseAppInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseAppInventories_TenantId_HasExpiredCredentials",
                table: "EnterpriseAppInventories",
                columns: new[] { "TenantId", "HasExpiredCredentials" });

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseAppInventories_TenantId_HasHighPrivilegePermissions",
                table: "EnterpriseAppInventories",
                columns: new[] { "TenantId", "HasHighPrivilegePermissions" });

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseAppInventories_TenantId_SnapshotId",
                table: "EnterpriseAppInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeOrganizationInventories_SnapshotId",
                table: "ExchangeOrganizationInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeOrganizationInventories_TenantId_SnapshotId",
                table: "ExchangeOrganizationInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupInventories_ObjectId",
                table: "GroupInventories",
                column: "ObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInventories_SnapshotId",
                table: "GroupInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInventories_TenantId_IsSecurityGroup",
                table: "GroupInventories",
                columns: new[] { "TenantId", "IsSecurityGroup" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupInventories_TenantId_SnapshotId",
                table: "GroupInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_HighRiskFindingInventories_FindingType",
                table: "HighRiskFindingInventories",
                column: "FindingType");

            migrationBuilder.CreateIndex(
                name: "IX_HighRiskFindingInventories_SnapshotId",
                table: "HighRiskFindingInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_HighRiskFindingInventories_TenantId_Severity",
                table: "HighRiskFindingInventories",
                columns: new[] { "TenantId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_HighRiskFindingInventories_TenantId_SnapshotId",
                table: "HighRiskFindingInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_TenantId",
                table: "InventorySnapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_TenantId_CollectedAt",
                table: "InventorySnapshots",
                columns: new[] { "TenantId", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshots_TenantId_Domain",
                table: "InventorySnapshots",
                columns: new[] { "TenantId", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseSubscriptions_SkuPartNumber",
                table: "LicenseSubscriptions",
                column: "SkuPartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseSubscriptions_SnapshotId",
                table: "LicenseSubscriptions",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseSubscriptions_TenantId_SnapshotId",
                table: "LicenseSubscriptions",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseUtilizationInventories_SnapshotId",
                table: "LicenseUtilizationInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseUtilizationInventories_TenantId_SnapshotId",
                table: "LicenseUtilizationInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedIdentityInventories_SnapshotId",
                table: "ManagedIdentityInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedIdentityInventories_TenantId_SnapshotId",
                table: "ManagedIdentityInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamedLocationInventories_SnapshotId",
                table: "NamedLocationInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_NamedLocationInventories_TenantId_SnapshotId",
                table: "NamedLocationInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConsentInventories_SnapshotId",
                table: "OAuthConsentInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConsentInventories_TenantId_SnapshotId",
                table: "OAuthConsentInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_SecureScoreInventories_SnapshotId",
                table: "SecureScoreInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SecureScoreInventories_TenantId_SnapshotId",
                table: "SecureScoreInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_SensitivityLabelInventories_SnapshotId",
                table: "SensitivityLabelInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SensitivityLabelInventories_TenantId_SnapshotId",
                table: "SensitivityLabelInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalInventories_AppId",
                table: "ServicePrincipalInventories",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalInventories_SnapshotId",
                table: "ServicePrincipalInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalInventories_TenantId_HasHighPrivilegePermissions",
                table: "ServicePrincipalInventories",
                columns: new[] { "TenantId", "HasHighPrivilegePermissions" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalInventories_TenantId_SnapshotId",
                table: "ServicePrincipalInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharePointSettingsInventories_SnapshotId",
                table: "SharePointSettingsInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SharePointSettingsInventories_TenantId_SnapshotId",
                table: "SharePointSettingsInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_SharePointSiteInventories_SiteId",
                table: "SharePointSiteInventories",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_SharePointSiteInventories_SnapshotId",
                table: "SharePointSiteInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SharePointSiteInventories_TenantId_HasExternalSharing",
                table: "SharePointSiteInventories",
                columns: new[] { "TenantId", "HasExternalSharing" });

            migrationBuilder.CreateIndex(
                name: "IX_SharePointSiteInventories_TenantId_SnapshotId",
                table: "SharePointSiteInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamsInventories_SnapshotId",
                table: "TeamsInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsInventories_TeamId",
                table: "TeamsInventories",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsInventories_TenantId_SnapshotId",
                table: "TeamsInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamsSettingsInventories_SnapshotId",
                table: "TeamsSettingsInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsSettingsInventories_TenantId_SnapshotId",
                table: "TeamsSettingsInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInfos_SnapshotId",
                table: "TenantInfos",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInfos_TenantId_SnapshotId",
                table: "TenantInfos",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_ObjectId",
                table: "UserInventories",
                column: "ObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_SnapshotId",
                table: "UserInventories",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_TenantId_HasE5License",
                table: "UserInventories",
                columns: new[] { "TenantId", "HasE5License" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_TenantId_IsPrivileged",
                table: "UserInventories",
                columns: new[] { "TenantId", "IsPrivileged" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_TenantId_RiskLevel",
                table: "UserInventories",
                columns: new[] { "TenantId", "RiskLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_TenantId_SnapshotId",
                table: "UserInventories",
                columns: new[] { "TenantId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_TenantId_UserType",
                table: "UserInventories",
                columns: new[] { "TenantId", "UserType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_UserPrincipalName",
                table: "UserInventories",
                column: "UserPrincipalName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogInventories");

            migrationBuilder.DropTable(
                name: "AuthenticationMethodInventories");

            migrationBuilder.DropTable(
                name: "ComplianceInventories");

            migrationBuilder.DropTable(
                name: "CompliancePolicyInventories");

            migrationBuilder.DropTable(
                name: "ConditionalAccessPolicyInventories");

            migrationBuilder.DropTable(
                name: "ConfigurationProfileInventories");

            migrationBuilder.DropTable(
                name: "DefenderForCloudAppsInventories");

            migrationBuilder.DropTable(
                name: "DefenderForEndpointInventories");

            migrationBuilder.DropTable(
                name: "DefenderForIdentityInventories");

            migrationBuilder.DropTable(
                name: "DefenderForOffice365Inventories");

            migrationBuilder.DropTable(
                name: "DeviceInventories");

            migrationBuilder.DropTable(
                name: "DirectoryRoleInventories");

            migrationBuilder.DropTable(
                name: "DlpPolicyInventories");

            migrationBuilder.DropTable(
                name: "EnterpriseAppInventories");

            migrationBuilder.DropTable(
                name: "ExchangeOrganizationInventories");

            migrationBuilder.DropTable(
                name: "GroupInventories");

            migrationBuilder.DropTable(
                name: "HighRiskFindingInventories");

            migrationBuilder.DropTable(
                name: "LicenseSubscriptions");

            migrationBuilder.DropTable(
                name: "LicenseUtilizationInventories");

            migrationBuilder.DropTable(
                name: "ManagedIdentityInventories");

            migrationBuilder.DropTable(
                name: "NamedLocationInventories");

            migrationBuilder.DropTable(
                name: "OAuthConsentInventories");

            migrationBuilder.DropTable(
                name: "SecureScoreInventories");

            migrationBuilder.DropTable(
                name: "SensitivityLabelInventories");

            migrationBuilder.DropTable(
                name: "ServicePrincipalInventories");

            migrationBuilder.DropTable(
                name: "SharePointSettingsInventories");

            migrationBuilder.DropTable(
                name: "SharePointSiteInventories");

            migrationBuilder.DropTable(
                name: "TeamsInventories");

            migrationBuilder.DropTable(
                name: "TeamsSettingsInventories");

            migrationBuilder.DropTable(
                name: "TenantInfos");

            migrationBuilder.DropTable(
                name: "UserInventories");

            migrationBuilder.DropTable(
                name: "InventorySnapshots");
        }
    }
}
