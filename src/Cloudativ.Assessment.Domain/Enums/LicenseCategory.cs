namespace Cloudativ.Assessment.Domain.Enums;

/// <summary>
/// Categorizes Microsoft 365 license types for inventory tracking and utilization analysis.
/// </summary>
public enum LicenseCategory
{
    Unknown = 0,

    // Enterprise Licenses
    EnterpriseE1 = 10,
    EnterpriseE3 = 11,
    EnterpriseE5 = 12,

    // Business Licenses
    BusinessBasic = 20,
    BusinessStandard = 21,
    BusinessPremium = 22,

    // Frontline Licenses
    FrontlineF1 = 30,
    FrontlineF3 = 31,

    // Education Licenses
    EducationA1 = 40,
    EducationA3 = 41,
    EducationA5 = 42,

    // Government Licenses
    GovernmentG1 = 50,
    GovernmentG3 = 51,
    GovernmentG5 = 52,

    // Defender Add-ons
    DefenderForEndpointP1 = 100,
    DefenderForEndpointP2 = 101,
    DefenderForOffice365P1 = 102,
    DefenderForOffice365P2 = 103,
    DefenderForIdentity = 104,
    DefenderForCloudApps = 105,

    // Identity Add-ons
    AzureADPremiumP1 = 110,
    AzureADPremiumP2 = 111,
    EntraIdP1 = 112,
    EntraIdP2 = 113,

    // Device Management
    IntuneP1 = 120,
    IntuneP2 = 121,

    // Compliance Add-ons
    PurviewComplianceP1 = 130,
    PurviewComplianceP2 = 131,

    // Power BI
    PowerBIPro = 140,
    PowerBIPremium = 141,

    // Exchange
    ExchangeOnlinePlan1 = 200,
    ExchangeOnlinePlan2 = 201,
    ExchangeOnlineKiosk = 202,

    // SharePoint/OneDrive
    SharePointOnlinePlan1 = 210,
    SharePointOnlinePlan2 = 211,
    OneDriveForBusiness = 212,

    // Teams/Communication
    TeamsEssentials = 220,
    TeamsPhone = 221,
    AudioConferencing = 222,

    // Visio/Project
    VisioOnline = 230,
    ProjectOnline = 231,

    // Other
    Other = 999
}

public static class LicenseCategoryExtensions
{
    /// <summary>
    /// Gets the display name for a license category.
    /// </summary>
    public static string GetDisplayName(this LicenseCategory category) => category switch
    {
        LicenseCategory.Unknown => "Unknown",

        // Enterprise
        LicenseCategory.EnterpriseE1 => "Microsoft 365 E1",
        LicenseCategory.EnterpriseE3 => "Microsoft 365 E3",
        LicenseCategory.EnterpriseE5 => "Microsoft 365 E5",

        // Business
        LicenseCategory.BusinessBasic => "Microsoft 365 Business Basic",
        LicenseCategory.BusinessStandard => "Microsoft 365 Business Standard",
        LicenseCategory.BusinessPremium => "Microsoft 365 Business Premium",

        // Frontline
        LicenseCategory.FrontlineF1 => "Microsoft 365 F1",
        LicenseCategory.FrontlineF3 => "Microsoft 365 F3",

        // Education
        LicenseCategory.EducationA1 => "Microsoft 365 A1",
        LicenseCategory.EducationA3 => "Microsoft 365 A3",
        LicenseCategory.EducationA5 => "Microsoft 365 A5",

        // Government
        LicenseCategory.GovernmentG1 => "Microsoft 365 G1",
        LicenseCategory.GovernmentG3 => "Microsoft 365 G3",
        LicenseCategory.GovernmentG5 => "Microsoft 365 G5",

        // Defender
        LicenseCategory.DefenderForEndpointP1 => "Defender for Endpoint P1",
        LicenseCategory.DefenderForEndpointP2 => "Defender for Endpoint P2",
        LicenseCategory.DefenderForOffice365P1 => "Defender for Office 365 P1",
        LicenseCategory.DefenderForOffice365P2 => "Defender for Office 365 P2",
        LicenseCategory.DefenderForIdentity => "Defender for Identity",
        LicenseCategory.DefenderForCloudApps => "Defender for Cloud Apps",

        // Identity
        LicenseCategory.AzureADPremiumP1 => "Azure AD Premium P1",
        LicenseCategory.AzureADPremiumP2 => "Azure AD Premium P2",
        LicenseCategory.EntraIdP1 => "Entra ID P1",
        LicenseCategory.EntraIdP2 => "Entra ID P2",

        // Intune
        LicenseCategory.IntuneP1 => "Intune P1",
        LicenseCategory.IntuneP2 => "Intune P2",

        // Compliance
        LicenseCategory.PurviewComplianceP1 => "Purview Compliance P1",
        LicenseCategory.PurviewComplianceP2 => "Purview Compliance P2",

        // Power BI
        LicenseCategory.PowerBIPro => "Power BI Pro",
        LicenseCategory.PowerBIPremium => "Power BI Premium",

        // Exchange
        LicenseCategory.ExchangeOnlinePlan1 => "Exchange Online Plan 1",
        LicenseCategory.ExchangeOnlinePlan2 => "Exchange Online Plan 2",
        LicenseCategory.ExchangeOnlineKiosk => "Exchange Online Kiosk",

        // SharePoint
        LicenseCategory.SharePointOnlinePlan1 => "SharePoint Online Plan 1",
        LicenseCategory.SharePointOnlinePlan2 => "SharePoint Online Plan 2",
        LicenseCategory.OneDriveForBusiness => "OneDrive for Business",

        // Teams
        LicenseCategory.TeamsEssentials => "Teams Essentials",
        LicenseCategory.TeamsPhone => "Teams Phone",
        LicenseCategory.AudioConferencing => "Audio Conferencing",

        // Visio/Project
        LicenseCategory.VisioOnline => "Visio Online",
        LicenseCategory.ProjectOnline => "Project Online",

        LicenseCategory.Other => "Other",
        _ => category.ToString()
    };

    /// <summary>
    /// Gets the short display name for a license category (for badges/chips).
    /// </summary>
    public static string GetShortName(this LicenseCategory category) => category switch
    {
        LicenseCategory.EnterpriseE1 => "E1",
        LicenseCategory.EnterpriseE3 => "E3",
        LicenseCategory.EnterpriseE5 => "E5",
        LicenseCategory.BusinessBasic => "Basic",
        LicenseCategory.BusinessStandard => "Standard",
        LicenseCategory.BusinessPremium => "Premium",
        LicenseCategory.FrontlineF1 => "F1",
        LicenseCategory.FrontlineF3 => "F3",
        LicenseCategory.EducationA1 => "A1",
        LicenseCategory.EducationA3 => "A3",
        LicenseCategory.EducationA5 => "A5",
        LicenseCategory.GovernmentG1 => "G1",
        LicenseCategory.GovernmentG3 => "G3",
        LicenseCategory.GovernmentG5 => "G5",
        _ => category.GetDisplayName()
    };

    /// <summary>
    /// Gets the CSS color class for a license category.
    /// </summary>
    public static string GetColorHex(this LicenseCategory category) => category switch
    {
        // Enterprise - Blue shades
        LicenseCategory.EnterpriseE1 => "#64B5F6",
        LicenseCategory.EnterpriseE3 => "#1976D2",
        LicenseCategory.EnterpriseE5 => "#0D47A1",

        // Business - Green shades
        LicenseCategory.BusinessBasic => "#81C784",
        LicenseCategory.BusinessStandard => "#4CAF50",
        LicenseCategory.BusinessPremium => "#2E7D32",

        // Frontline - Orange shades
        LicenseCategory.FrontlineF1 => "#FFB74D",
        LicenseCategory.FrontlineF3 => "#F57C00",

        // Education - Purple shades
        LicenseCategory.EducationA1 => "#BA68C8",
        LicenseCategory.EducationA3 => "#9C27B0",
        LicenseCategory.EducationA5 => "#6A1B9A",

        // Government - Teal shades
        LicenseCategory.GovernmentG1 => "#4DD0E1",
        LicenseCategory.GovernmentG3 => "#00BCD4",
        LicenseCategory.GovernmentG5 => "#006064",

        // Defender - Red shades
        LicenseCategory.DefenderForEndpointP1 or
        LicenseCategory.DefenderForEndpointP2 or
        LicenseCategory.DefenderForOffice365P1 or
        LicenseCategory.DefenderForOffice365P2 or
        LicenseCategory.DefenderForIdentity or
        LicenseCategory.DefenderForCloudApps => "#D32F2F",

        // Identity - Indigo
        LicenseCategory.AzureADPremiumP1 or
        LicenseCategory.AzureADPremiumP2 or
        LicenseCategory.EntraIdP1 or
        LicenseCategory.EntraIdP2 => "#3F51B5",

        // Default
        _ => "#757575"
    };

    /// <summary>
    /// Gets the tier group for a license category (for grouping in UI).
    /// </summary>
    public static string GetTierGroup(this LicenseCategory category) => category switch
    {
        LicenseCategory.EnterpriseE1 or LicenseCategory.EnterpriseE3 or LicenseCategory.EnterpriseE5 => "Enterprise",
        LicenseCategory.BusinessBasic or LicenseCategory.BusinessStandard or LicenseCategory.BusinessPremium => "Business",
        LicenseCategory.FrontlineF1 or LicenseCategory.FrontlineF3 => "Frontline",
        LicenseCategory.EducationA1 or LicenseCategory.EducationA3 or LicenseCategory.EducationA5 => "Education",
        LicenseCategory.GovernmentG1 or LicenseCategory.GovernmentG3 or LicenseCategory.GovernmentG5 => "Government",
        LicenseCategory.DefenderForEndpointP1 or LicenseCategory.DefenderForEndpointP2 or
        LicenseCategory.DefenderForOffice365P1 or LicenseCategory.DefenderForOffice365P2 or
        LicenseCategory.DefenderForIdentity or LicenseCategory.DefenderForCloudApps => "Security Add-ons",
        LicenseCategory.AzureADPremiumP1 or LicenseCategory.AzureADPremiumP2 or
        LicenseCategory.EntraIdP1 or LicenseCategory.EntraIdP2 => "Identity Add-ons",
        LicenseCategory.IntuneP1 or LicenseCategory.IntuneP2 => "Device Management",
        LicenseCategory.PurviewComplianceP1 or LicenseCategory.PurviewComplianceP2 => "Compliance Add-ons",
        LicenseCategory.PowerBIPro or LicenseCategory.PowerBIPremium => "Analytics",
        LicenseCategory.ExchangeOnlinePlan1 or LicenseCategory.ExchangeOnlinePlan2 or
        LicenseCategory.ExchangeOnlineKiosk => "Exchange",
        LicenseCategory.SharePointOnlinePlan1 or LicenseCategory.SharePointOnlinePlan2 or
        LicenseCategory.OneDriveForBusiness => "SharePoint & OneDrive",
        LicenseCategory.TeamsEssentials or LicenseCategory.TeamsPhone or LicenseCategory.AudioConferencing => "Teams",
        LicenseCategory.VisioOnline or LicenseCategory.ProjectOnline => "Productivity Apps",
        _ => "Other"
    };

    /// <summary>
    /// Checks if the category is a primary user license (vs add-on).
    /// </summary>
    public static bool IsPrimaryLicense(this LicenseCategory category) => category switch
    {
        LicenseCategory.EnterpriseE1 or LicenseCategory.EnterpriseE3 or LicenseCategory.EnterpriseE5 or
        LicenseCategory.BusinessBasic or LicenseCategory.BusinessStandard or LicenseCategory.BusinessPremium or
        LicenseCategory.FrontlineF1 or LicenseCategory.FrontlineF3 or
        LicenseCategory.EducationA1 or LicenseCategory.EducationA3 or LicenseCategory.EducationA5 or
        LicenseCategory.GovernmentG1 or LicenseCategory.GovernmentG3 or LicenseCategory.GovernmentG5 => true,
        _ => false
    };

    /// <summary>
    /// Gets the priority/tier ranking for a license (higher = more features).
    /// </summary>
    public static int GetTierRanking(this LicenseCategory category) => category switch
    {
        // Enterprise - highest tier
        LicenseCategory.EnterpriseE5 => 100,
        LicenseCategory.EnterpriseE3 => 80,
        LicenseCategory.EnterpriseE1 => 60,

        // Business
        LicenseCategory.BusinessPremium => 75,
        LicenseCategory.BusinessStandard => 55,
        LicenseCategory.BusinessBasic => 40,

        // Education (equivalent to enterprise)
        LicenseCategory.EducationA5 => 100,
        LicenseCategory.EducationA3 => 80,
        LicenseCategory.EducationA1 => 60,

        // Government (equivalent to enterprise)
        LicenseCategory.GovernmentG5 => 100,
        LicenseCategory.GovernmentG3 => 80,
        LicenseCategory.GovernmentG1 => 60,

        // Frontline - lowest tier
        LicenseCategory.FrontlineF3 => 35,
        LicenseCategory.FrontlineF1 => 20,

        _ => 0
    };
}
