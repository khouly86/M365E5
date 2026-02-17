using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloudativ.Assessment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLicenseCategorySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllLicenseCategoriesJson",
                table: "UserInventories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBusinessBasic",
                table: "UserInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBusinessPremium",
                table: "UserInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBusinessStandard",
                table: "UserInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasEducationLicense",
                table: "UserInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasFrontlineLicense",
                table: "UserInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasGovernmentLicense",
                table: "UserInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryLicenseCategory",
                table: "UserInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryLicenseTierGroup",
                table: "UserInventories",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AddOnLicensesAssigned",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AddOnLicensesTotal",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BusinessLicensesAssigned",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BusinessLicensesTotal",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EducationLicensesAssigned",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EducationLicensesTotal",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnterpriseLicensesAssigned",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnterpriseLicensesTotal",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FrontlineLicensesAssigned",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FrontlineLicensesTotal",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GovernmentLicensesAssigned",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GovernmentLicensesTotal",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LicenseSummaryByCategoryJson",
                table: "LicenseUtilizationInventories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OverallUtilizationPercentage",
                table: "LicenseUtilizationInventories",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "TopWasteByCategoryJson",
                table: "LicenseUtilizationInventories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalAssignedLicenses",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalAvailableLicenses",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEstimatedAnnualWaste",
                table: "LicenseUtilizationInventories",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEstimatedMonthlyWaste",
                table: "LicenseUtilizationInventories",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalLicenseCategories",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalLicenses",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPrimaryLicenseUsers",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalSuspendedLicenses",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalUnlicensedUsers",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalUsersWithoutCa",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalUsersWithoutDefender",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalUsersWithoutMfa",
                table: "LicenseUtilizationInventories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMonthlyPricePerUser",
                table: "LicenseSubscriptions",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IncludedFeaturesJson",
                table: "LicenseSubscriptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryLicense",
                table: "LicenseSubscriptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LicenseCategory",
                table: "LicenseSubscriptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TierGroup",
                table: "LicenseSubscriptions",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LicenseCategoryUtilizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LicenseCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryDisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TierGroup = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TotalLicenses = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedLicenses = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableLicenses = table.Column<int>(type: "INTEGER", nullable: false),
                    SuspendedLicenses = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalUsersWithLicense = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveUsersWithLicense = table.Column<int>(type: "INTEGER", nullable: false),
                    DisabledUsersWithLicense = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithMfaEnabled = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithoutMfa = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithConditionalAccess = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithoutCa = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithPimCoverage = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithIdentityProtection = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithDefenderForEndpoint = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithDefenderForOffice = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithDefenderForIdentity = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithDefenderForCloudApps = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithPurviewLabels = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithDlp = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithRetention = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithEDiscovery = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithInsiderRisk = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithTeamsPhoneSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithAudioConferencing = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersWithPowerBI = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    IdentityFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    SecurityFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    ComplianceFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    ProductivityFeatureUtilization = table.Column<double>(type: "REAL", nullable: false),
                    EstimatedMonthlyPricePerUser = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalMonthlyLicenseCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EstimatedMonthlyWaste = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EstimatedAnnualWaste = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PotentialSavingsIfDowngraded = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UsersEligibleForDowngrade = table.Column<int>(type: "INTEGER", nullable: false),
                    IncludedFeaturesJson = table.Column<string>(type: "TEXT", nullable: true),
                    UsedFeaturesJson = table.Column<string>(type: "TEXT", nullable: true),
                    UnusedFeaturesJson = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureUtilizationBreakdownJson = table.Column<string>(type: "TEXT", nullable: true),
                    TopUsersWithoutMfaJson = table.Column<string>(type: "TEXT", nullable: true),
                    TopUsersWithoutCaJson = table.Column<string>(type: "TEXT", nullable: true),
                    TopUsersNotUsingSecurityJson = table.Column<string>(type: "TEXT", nullable: true),
                    UsersEligibleForDowngradeJson = table.Column<string>(type: "TEXT", nullable: true),
                    RecommendationsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CriticalRecommendations = table.Column<int>(type: "INTEGER", nullable: false),
                    HighRecommendations = table.Column<int>(type: "INTEGER", nullable: false),
                    MediumRecommendations = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseCategoryUtilizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicenseCategoryUtilizations_InventorySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "InventorySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LicenseCategoryUtilizations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventories_TenantId_PrimaryLicenseCategory",
                table: "UserInventories",
                columns: new[] { "TenantId", "PrimaryLicenseCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseSubscriptions_TenantId_LicenseCategory",
                table: "LicenseSubscriptions",
                columns: new[] { "TenantId", "LicenseCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseCategoryUtilizations_SnapshotId_LicenseCategory",
                table: "LicenseCategoryUtilizations",
                columns: new[] { "SnapshotId", "LicenseCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseCategoryUtilizations_TenantId_LicenseCategory",
                table: "LicenseCategoryUtilizations",
                columns: new[] { "TenantId", "LicenseCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_LicenseCategoryUtilizations_TenantId_SnapshotId",
                table: "LicenseCategoryUtilizations",
                columns: new[] { "TenantId", "SnapshotId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicenseCategoryUtilizations");

            migrationBuilder.DropIndex(
                name: "IX_UserInventories_TenantId_PrimaryLicenseCategory",
                table: "UserInventories");

            migrationBuilder.DropIndex(
                name: "IX_LicenseSubscriptions_TenantId_LicenseCategory",
                table: "LicenseSubscriptions");

            migrationBuilder.DropColumn(
                name: "AllLicenseCategoriesJson",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "HasBusinessBasic",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "HasBusinessPremium",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "HasBusinessStandard",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "HasEducationLicense",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "HasFrontlineLicense",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "HasGovernmentLicense",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "PrimaryLicenseCategory",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "PrimaryLicenseTierGroup",
                table: "UserInventories");

            migrationBuilder.DropColumn(
                name: "AddOnLicensesAssigned",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "AddOnLicensesTotal",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "BusinessLicensesAssigned",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "BusinessLicensesTotal",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "EducationLicensesAssigned",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "EducationLicensesTotal",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "EnterpriseLicensesAssigned",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "EnterpriseLicensesTotal",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "FrontlineLicensesAssigned",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "FrontlineLicensesTotal",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "GovernmentLicensesAssigned",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "GovernmentLicensesTotal",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "LicenseSummaryByCategoryJson",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "OverallUtilizationPercentage",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TopWasteByCategoryJson",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalAssignedLicenses",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalAvailableLicenses",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalEstimatedAnnualWaste",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalEstimatedMonthlyWaste",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalLicenseCategories",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalLicenses",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalPrimaryLicenseUsers",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalSuspendedLicenses",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalUnlicensedUsers",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalUsersWithoutCa",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalUsersWithoutDefender",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "TotalUsersWithoutMfa",
                table: "LicenseUtilizationInventories");

            migrationBuilder.DropColumn(
                name: "EstimatedMonthlyPricePerUser",
                table: "LicenseSubscriptions");

            migrationBuilder.DropColumn(
                name: "IncludedFeaturesJson",
                table: "LicenseSubscriptions");

            migrationBuilder.DropColumn(
                name: "IsPrimaryLicense",
                table: "LicenseSubscriptions");

            migrationBuilder.DropColumn(
                name: "LicenseCategory",
                table: "LicenseSubscriptions");

            migrationBuilder.DropColumn(
                name: "TierGroup",
                table: "LicenseSubscriptions");
        }
    }
}
