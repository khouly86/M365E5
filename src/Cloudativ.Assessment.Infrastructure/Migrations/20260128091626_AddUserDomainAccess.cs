using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloudativ.Assessment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDomainAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExternalAuth = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExternalAuthProvider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ExternalAuthId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FriendlyName = table.Column<string>(type: "TEXT", nullable: true),
                    Xml = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AzureTenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OnboardingStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Industry = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    AuthenticationType = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SecretId = table.Column<string>(type: "TEXT", nullable: true),
                    ClientSecretEncrypted = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    OpenAiEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenAiApiKeyEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedComplianceStandards = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDomainAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Domain = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDomainAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDomainAccess_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    InitiatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SummaryScoresJson = table.Column<string>(type: "TEXT", nullable: true),
                    OverallScore = table.Column<int>(type: "INTEGER", nullable: true),
                    AiAnalysisJson = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    AssessedDomainsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Plan = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MonthlyAssessmentLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    AssessmentsUsedThisMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    StripePriceId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    YearlyPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AutoScheduleEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScheduleCron = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EnabledDomainsJson = table.Column<string>(type: "TEXT", nullable: true),
                    EmailNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationEmails = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    IncludeRawDataInReports = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReportLogoBase64 = table.Column<string>(type: "TEXT", nullable: true),
                    CustomReportHeader = table.Column<string>(type: "TEXT", nullable: true),
                    UseAiScoring = table.Column<bool>(type: "INTEGER", nullable: false),
                    AiModel = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ManualDkimConfig = table.Column<string>(type: "TEXT", nullable: true),
                    ManualDmarcConfig = table.Column<string>(type: "TEXT", nullable: true),
                    ManualSpfConfig = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantUserAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantRole = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUserAccess_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantUserAccess_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Domain = table.Column<int>(type: "INTEGER", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    EvidenceJson = table.Column<string>(type: "TEXT", nullable: true),
                    Remediation = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    References = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    AffectedResources = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    IsCompliant = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CheckName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_AssessmentRuns_AssessmentRunId",
                        column: x => x.AssessmentRunId,
                        principalTable: "AssessmentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Standard = table.Column<int>(type: "INTEGER", nullable: false),
                    ComplianceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalControls = table.Column<int>(type: "INTEGER", nullable: false),
                    CompliantControls = table.Column<int>(type: "INTEGER", nullable: false),
                    PartiallyCompliantControls = table.Column<int>(type: "INTEGER", nullable: false),
                    NonCompliantControls = table.Column<int>(type: "INTEGER", nullable: false),
                    ComplianceGapsJson = table.Column<string>(type: "TEXT", nullable: true),
                    RecommendationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CompliantAreasJson = table.Column<string>(type: "TEXT", nullable: true),
                    AiModelUsed = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TokensUsed = table.Column<int>(type: "INTEGER", nullable: true),
                    RawResponseJson = table.Column<string>(type: "TEXT", nullable: true),
                    StandardDocumentVersion = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernanceAnalyses_AssessmentRuns_AssessmentRunId",
                        column: x => x.AssessmentRunId,
                        principalTable: "AssessmentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GovernanceAnalyses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RawSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Domain = table.Column<int>(type: "INTEGER", nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayloadSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    IsCompressed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RawSnapshots_AssessmentRuns_AssessmentRunId",
                        column: x => x.AssessmentRunId,
                        principalTable: "AssessmentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentRuns_StartedAt",
                table: "AssessmentRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentRuns_TenantId",
                table: "AssessmentRuns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentRuns_TenantId_StartedAt",
                table: "AssessmentRuns",
                columns: new[] { "TenantId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_AssessmentRunId",
                table: "Findings",
                column: "AssessmentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_AssessmentRunId_Domain",
                table: "Findings",
                columns: new[] { "AssessmentRunId", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Domain",
                table: "Findings",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Severity",
                table: "Findings",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceAnalyses_AssessmentRunId",
                table: "GovernanceAnalyses",
                column: "AssessmentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceAnalyses_AssessmentRunId_Standard",
                table: "GovernanceAnalyses",
                columns: new[] { "AssessmentRunId", "Standard" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceAnalyses_TenantId",
                table: "GovernanceAnalyses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceAnalyses_TenantId_Standard",
                table: "GovernanceAnalyses",
                columns: new[] { "TenantId", "Standard" });

            migrationBuilder.CreateIndex(
                name: "IX_RawSnapshots_AssessmentRunId",
                table: "RawSnapshots",
                column: "AssessmentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_RawSnapshots_AssessmentRunId_Domain",
                table: "RawSnapshots",
                columns: new[] { "AssessmentRunId", "Domain" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeCustomerId",
                table: "Subscriptions",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeSubscriptionId",
                table: "Subscriptions",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_AzureTenantId",
                table: "Tenants",
                column: "AzureTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Domain",
                table: "Tenants",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserAccess_AppUserId_TenantId",
                table: "TenantUserAccess",
                columns: new[] { "AppUserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserAccess_TenantId",
                table: "TenantUserAccess",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDomainAccess_AppUserId_Domain",
                table: "UserDomainAccess",
                columns: new[] { "AppUserId", "Domain" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "GovernanceAnalyses");

            migrationBuilder.DropTable(
                name: "RawSnapshots");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropTable(
                name: "TenantUserAccess");

            migrationBuilder.DropTable(
                name: "UserDomainAccess");

            migrationBuilder.DropTable(
                name: "AssessmentRuns");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
