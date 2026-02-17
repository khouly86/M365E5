using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Infrastructure.Services;

/// <summary>
/// Maps Microsoft 365 SKU Part Numbers to license categories.
/// </summary>
public static class LicenseSkuMapper
{
    /// <summary>
    /// Comprehensive mapping of Microsoft SKU Part Numbers to categories.
    /// </summary>
    private static readonly Dictionary<string, LicenseCategory> SkuMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // === ENTERPRISE LICENSES ===
        // E1
        ["STANDARDPACK"] = LicenseCategory.EnterpriseE1,
        ["O365_BUSINESS_ESSENTIALS"] = LicenseCategory.EnterpriseE1,

        // E3
        ["ENTERPRISEPACK"] = LicenseCategory.EnterpriseE3,
        ["SPE_E3"] = LicenseCategory.EnterpriseE3,
        ["MICROSOFT_365_E3"] = LicenseCategory.EnterpriseE3,
        ["M365_E3"] = LicenseCategory.EnterpriseE3,
        ["M365_E3_EEA_NOCREDIT"] = LicenseCategory.EnterpriseE3,

        // E5
        ["ENTERPRISEPREMIUM"] = LicenseCategory.EnterpriseE5,
        ["ENTERPRISEPREMIUM_NOPSTNCONF"] = LicenseCategory.EnterpriseE5,
        ["SPE_E5"] = LicenseCategory.EnterpriseE5,
        ["MICROSOFT_365_E5"] = LicenseCategory.EnterpriseE5,
        ["M365_E5"] = LicenseCategory.EnterpriseE5,
        ["M365_E5_EEA_NOCREDIT"] = LicenseCategory.EnterpriseE5,

        // === BUSINESS LICENSES ===
        // Business Basic
        ["O365_BUSINESS_ESSENTIALS"] = LicenseCategory.BusinessBasic,
        ["SMB_BUSINESS_ESSENTIALS"] = LicenseCategory.BusinessBasic,
        ["M365_BUSINESS_BASIC"] = LicenseCategory.BusinessBasic,
        ["BUSINESS_BASIC"] = LicenseCategory.BusinessBasic,

        // Business Standard
        ["O365_BUSINESS_PREMIUM"] = LicenseCategory.BusinessStandard,
        ["SMB_BUSINESS"] = LicenseCategory.BusinessStandard,
        ["M365_BUSINESS_STANDARD"] = LicenseCategory.BusinessStandard,
        ["BUSINESS_STANDARD"] = LicenseCategory.BusinessStandard,

        // Business Premium
        ["SPB"] = LicenseCategory.BusinessPremium,
        ["SMB_BUSINESS_PREMIUM"] = LicenseCategory.BusinessPremium,
        ["M365_BUSINESS_PREMIUM"] = LicenseCategory.BusinessPremium,
        ["BUSINESS_PREMIUM"] = LicenseCategory.BusinessPremium,

        // === FRONTLINE LICENSES ===
        // F1
        ["DESKLESSPACK"] = LicenseCategory.FrontlineF1,
        ["M365_F1"] = LicenseCategory.FrontlineF1,
        ["SPE_F1"] = LicenseCategory.FrontlineF1,
        ["O365_F1"] = LicenseCategory.FrontlineF1,

        // F3
        ["M365_F3"] = LicenseCategory.FrontlineF3,
        ["SPE_F3"] = LicenseCategory.FrontlineF3,
        ["O365_F3"] = LicenseCategory.FrontlineF3,

        // === EDUCATION LICENSES ===
        // A1
        ["STANDARDWOFFPACK_STUDENT"] = LicenseCategory.EducationA1,
        ["STANDARDWOFFPACK_FACULTY"] = LicenseCategory.EducationA1,
        ["STANDARDWOFFPACK_IW_STUDENT"] = LicenseCategory.EducationA1,
        ["STANDARDWOFFPACK_IW_FACULTY"] = LicenseCategory.EducationA1,

        // A3
        ["M365EDU_A3_STUDENT"] = LicenseCategory.EducationA3,
        ["M365EDU_A3_FACULTY"] = LicenseCategory.EducationA3,
        ["ENTERPRISEPACK_STUDENT"] = LicenseCategory.EducationA3,
        ["ENTERPRISEPACK_FACULTY"] = LicenseCategory.EducationA3,

        // A5
        ["M365EDU_A5_STUDENT"] = LicenseCategory.EducationA5,
        ["M365EDU_A5_FACULTY"] = LicenseCategory.EducationA5,
        ["ENTERPRISEPREMIUM_STUDENT"] = LicenseCategory.EducationA5,
        ["ENTERPRISEPREMIUM_FACULTY"] = LicenseCategory.EducationA5,

        // === GOVERNMENT LICENSES ===
        // G1
        ["STANDARDPACK_GOV"] = LicenseCategory.GovernmentG1,
        ["M365_G1"] = LicenseCategory.GovernmentG1,

        // G3
        ["ENTERPRISEPACK_GOV"] = LicenseCategory.GovernmentG3,
        ["M365_G3_GOV"] = LicenseCategory.GovernmentG3,

        // G5
        ["ENTERPRISEPREMIUM_GOV"] = LicenseCategory.GovernmentG5,
        ["M365_G5_GOV"] = LicenseCategory.GovernmentG5,

        // === DEFENDER ADD-ONS ===
        ["DEFENDER_ENDPOINT_P1"] = LicenseCategory.DefenderForEndpointP1,
        ["MDATP_XPLAT"] = LicenseCategory.DefenderForEndpointP1,
        ["DEFENDER_ENDPOINT_P2"] = LicenseCategory.DefenderForEndpointP2,
        ["WIN_DEF_ATP"] = LicenseCategory.DefenderForEndpointP2,
        ["ATP_ENTERPRISE"] = LicenseCategory.DefenderForOffice365P1,
        ["THREAT_INTELLIGENCE"] = LicenseCategory.DefenderForOffice365P2,
        ["ATP_ENTERPRISE_GOV"] = LicenseCategory.DefenderForOffice365P1,
        ["ATA"] = LicenseCategory.DefenderForIdentity,
        ["IDENTITY_THREAT_PROTECTION"] = LicenseCategory.DefenderForIdentity,
        ["ADALLOM_STANDALONE"] = LicenseCategory.DefenderForCloudApps,
        ["ADALLOM_S_STANDALONE"] = LicenseCategory.DefenderForCloudApps,

        // === AZURE AD / ENTRA ID ===
        ["AAD_PREMIUM"] = LicenseCategory.AzureADPremiumP1,
        ["AAD_PREMIUM_P1"] = LicenseCategory.AzureADPremiumP1,
        ["ENTRA_ID_P1"] = LicenseCategory.EntraIdP1,
        ["AAD_PREMIUM_P2"] = LicenseCategory.AzureADPremiumP2,
        ["ENTRA_ID_P2"] = LicenseCategory.EntraIdP2,

        // === INTUNE ===
        ["INTUNE_A"] = LicenseCategory.IntuneP1,
        ["INTUNE_A_VL"] = LicenseCategory.IntuneP1,
        ["INTUNE_P1"] = LicenseCategory.IntuneP1,
        ["INTUNE_P2"] = LicenseCategory.IntuneP2,

        // === COMPLIANCE ===
        ["INFORMATION_PROTECTION_COMPLIANCE"] = LicenseCategory.PurviewComplianceP1,
        ["M365_COMPLIANCE_MANAGER_PREMIUM_ASSESSMENT_ADDON"] = LicenseCategory.PurviewComplianceP2,
        ["COMPLIANCEMANAGER_PREMIUM"] = LicenseCategory.PurviewComplianceP2,

        // === EXCHANGE ===
        ["EXCHANGESTANDARD"] = LicenseCategory.ExchangeOnlinePlan1,
        ["EXCHANGE_S_STANDARD"] = LicenseCategory.ExchangeOnlinePlan1,
        ["EXCHANGEENTERPRISE"] = LicenseCategory.ExchangeOnlinePlan2,
        ["EXCHANGE_S_ENTERPRISE"] = LicenseCategory.ExchangeOnlinePlan2,
        ["EXCHANGE_S_DESKLESS"] = LicenseCategory.ExchangeOnlineKiosk,
        ["EXCHANGEDESKLESS"] = LicenseCategory.ExchangeOnlineKiosk,

        // === SHAREPOINT ===
        ["SHAREPOINTSTANDARD"] = LicenseCategory.SharePointOnlinePlan1,
        ["SHAREPOINT_S_STANDARD"] = LicenseCategory.SharePointOnlinePlan1,
        ["SHAREPOINTENTERPRISE"] = LicenseCategory.SharePointOnlinePlan2,
        ["SHAREPOINT_S_ENTERPRISE"] = LicenseCategory.SharePointOnlinePlan2,
        ["ONEDRIVESTANDARD"] = LicenseCategory.OneDriveForBusiness,
        ["ONEDRIVE_BASIC"] = LicenseCategory.OneDriveForBusiness,
        ["ONEDRIVE_STANDARD"] = LicenseCategory.OneDriveForBusiness,

        // === POWER BI ===
        ["POWER_BI_PRO"] = LicenseCategory.PowerBIPro,
        ["BI_AZURE_P1"] = LicenseCategory.PowerBIPro,
        ["PBI_PREMIUM_PER_USER"] = LicenseCategory.PowerBIPremium,
        ["POWER_BI_PREMIUM_PER_USER"] = LicenseCategory.PowerBIPremium,

        // === TEAMS ===
        ["TEAMS_ESSENTIALS"] = LicenseCategory.TeamsEssentials,
        ["TEAMS_ESSENTIALS_AAD"] = LicenseCategory.TeamsEssentials,
        ["MCOEV"] = LicenseCategory.TeamsPhone,
        ["TEAMS_PHONE_STANDARD"] = LicenseCategory.TeamsPhone,
        ["MCOMEETADV"] = LicenseCategory.AudioConferencing,
        ["AUDIO_CONFERENCING"] = LicenseCategory.AudioConferencing,

        // === VISIO / PROJECT ===
        ["VISIO_PLAN1_DEPT"] = LicenseCategory.VisioOnline,
        ["VISIO_PLAN2_DEPT"] = LicenseCategory.VisioOnline,
        ["VISIOCLIENT"] = LicenseCategory.VisioOnline,
        ["PROJECTPROFESSIONAL"] = LicenseCategory.ProjectOnline,
        ["PROJECTPREMIUM"] = LicenseCategory.ProjectOnline,
        ["PROJECT_P1"] = LicenseCategory.ProjectOnline,
    };

    /// <summary>
    /// SKU ID to Category mapping for common SKU GUIDs.
    /// </summary>
    private static readonly Dictionary<string, LicenseCategory> SkuIdMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // E5
        ["06ebc4ee-1bb5-47dd-8120-11324bc54e06"] = LicenseCategory.EnterpriseE5, // Microsoft 365 E5
        ["26124093-3d78-432b-b5dc-48bf992543d5"] = LicenseCategory.EnterpriseE5, // Microsoft 365 E5 (no Audio Conferencing)
        ["c7df2760-2c81-4ef7-b578-5b5392b571df"] = LicenseCategory.EnterpriseE5, // Office 365 E5
        ["184efa21-98c3-4e5d-95ab-d07053a96e67"] = LicenseCategory.EnterpriseE5, // Microsoft 365 E5 Compliance
        ["eb56d846-a2d2-4a71-938a-bfc5e37fe5c9"] = LicenseCategory.EnterpriseE5, // Microsoft 365 E5 Security

        // E3
        ["05e9a617-0261-4cee-bb44-138d3ef5d965"] = LicenseCategory.EnterpriseE3, // Microsoft 365 E3
        ["6fd2c87f-b296-42f0-b197-1e91e994b900"] = LicenseCategory.EnterpriseE3, // Office 365 E3
        ["c5928f49-12ba-48f7-ada3-0d743a3601d5"] = LicenseCategory.EnterpriseE3, // VISIO PLAN 2

        // E1
        ["18181a46-0d4e-45cd-891e-60aabd171b4e"] = LicenseCategory.EnterpriseE1, // Office 365 E1

        // Business Premium
        ["cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46"] = LicenseCategory.BusinessPremium, // Microsoft 365 Business Premium

        // Business Standard
        ["f245ecc8-75af-4f8e-b61f-27d8114de5f3"] = LicenseCategory.BusinessStandard, // Microsoft 365 Business Standard

        // Business Basic
        ["3b555118-da6a-4418-894f-7df1e2096870"] = LicenseCategory.BusinessBasic, // Microsoft 365 Business Basic

        // F3
        ["66b55226-6b4f-492c-910c-a3b7a3c9d993"] = LicenseCategory.FrontlineF3, // Microsoft 365 F3

        // F1
        ["3f8e5c8e-0a5c-4e7c-8e1f-5f1e5f8f1f5e"] = LicenseCategory.FrontlineF1, // Microsoft 365 F1
    };

    /// <summary>
    /// Gets the license category for a given SKU Part Number.
    /// </summary>
    public static LicenseCategory GetCategory(string? skuPartNumber)
    {
        if (string.IsNullOrWhiteSpace(skuPartNumber))
            return LicenseCategory.Unknown;

        // Try exact match first
        if (SkuMappings.TryGetValue(skuPartNumber, out var category))
            return category;

        // Try pattern matching for common variations
        var sku = skuPartNumber.ToUpperInvariant();

        // Enterprise patterns
        if (sku.Contains("_E5") || sku.Contains("ENTERPRISEPREMIUM"))
            return LicenseCategory.EnterpriseE5;
        if (sku.Contains("_E3") || sku.Contains("ENTERPRISEPACK"))
            return LicenseCategory.EnterpriseE3;
        if (sku.Contains("_E1") || sku.Contains("STANDARDPACK"))
            return LicenseCategory.EnterpriseE1;

        // Business patterns
        if (sku.Contains("BUSINESS_PREMIUM") || sku.Contains("BP_") || sku.Contains("_BP"))
            return LicenseCategory.BusinessPremium;
        if (sku.Contains("BUSINESS_STANDARD"))
            return LicenseCategory.BusinessStandard;
        if (sku.Contains("BUSINESS_BASIC") || sku.Contains("BUSINESS_ESSENTIALS"))
            return LicenseCategory.BusinessBasic;

        // Frontline patterns
        if (sku.Contains("_F3"))
            return LicenseCategory.FrontlineF3;
        if (sku.Contains("_F1") || sku.Contains("DESKLESS"))
            return LicenseCategory.FrontlineF1;

        // Education patterns
        if (sku.Contains("_A5") || (sku.Contains("EDU") && sku.Contains("A5")))
            return LicenseCategory.EducationA5;
        if (sku.Contains("_A3") || (sku.Contains("EDU") && sku.Contains("A3")))
            return LicenseCategory.EducationA3;
        if (sku.Contains("_A1") || (sku.Contains("EDU") && sku.Contains("A1")))
            return LicenseCategory.EducationA1;

        // Government patterns
        if (sku.Contains("_GOV") || sku.Contains("_G5"))
            if (sku.Contains("G5") || sku.Contains("PREMIUM"))
                return LicenseCategory.GovernmentG5;
            else if (sku.Contains("G3") || sku.Contains("ENTERPRISE"))
                return LicenseCategory.GovernmentG3;
            else
                return LicenseCategory.GovernmentG1;

        // Defender patterns
        if (sku.Contains("DEFENDER") || sku.Contains("ATP"))
        {
            if (sku.Contains("ENDPOINT"))
            {
                if (sku.Contains("P2"))
                    return LicenseCategory.DefenderForEndpointP2;
                return LicenseCategory.DefenderForEndpointP1;
            }
            if (sku.Contains("OFFICE") || sku.Contains("O365"))
            {
                if (sku.Contains("P2"))
                    return LicenseCategory.DefenderForOffice365P2;
                return LicenseCategory.DefenderForOffice365P1;
            }
            if (sku.Contains("IDENTITY") || sku.Contains("ATA"))
                return LicenseCategory.DefenderForIdentity;
            if (sku.Contains("CLOUD") || sku.Contains("ADALLOM"))
                return LicenseCategory.DefenderForCloudApps;
        }

        // Identity patterns
        if (sku.Contains("AAD_PREMIUM") || sku.Contains("ENTRA"))
        {
            if (sku.Contains("P2"))
                return LicenseCategory.AzureADPremiumP2;
            return LicenseCategory.AzureADPremiumP1;
        }

        // Intune patterns
        if (sku.Contains("INTUNE"))
        {
            if (sku.Contains("P2"))
                return LicenseCategory.IntuneP2;
            return LicenseCategory.IntuneP1;
        }

        // Exchange patterns
        if (sku.Contains("EXCHANGE"))
        {
            if (sku.Contains("ENTERPRISE") || sku.Contains("PLAN2") || sku.Contains("_S_ENTERPRISE"))
                return LicenseCategory.ExchangeOnlinePlan2;
            if (sku.Contains("DESKLESS") || sku.Contains("KIOSK"))
                return LicenseCategory.ExchangeOnlineKiosk;
            return LicenseCategory.ExchangeOnlinePlan1;
        }

        // SharePoint patterns
        if (sku.Contains("SHAREPOINT"))
        {
            if (sku.Contains("ENTERPRISE") || sku.Contains("PLAN2"))
                return LicenseCategory.SharePointOnlinePlan2;
            return LicenseCategory.SharePointOnlinePlan1;
        }

        // OneDrive patterns
        if (sku.Contains("ONEDRIVE"))
            return LicenseCategory.OneDriveForBusiness;

        // Power BI patterns
        if (sku.Contains("POWER_BI") || sku.Contains("PBI"))
        {
            if (sku.Contains("PREMIUM"))
                return LicenseCategory.PowerBIPremium;
            return LicenseCategory.PowerBIPro;
        }

        // Teams patterns
        if (sku.Contains("TEAMS"))
        {
            if (sku.Contains("PHONE") || sku.Contains("MCOEV"))
                return LicenseCategory.TeamsPhone;
            if (sku.Contains("AUDIO") || sku.Contains("MCOMEETADV"))
                return LicenseCategory.AudioConferencing;
            if (sku.Contains("ESSENTIALS"))
                return LicenseCategory.TeamsEssentials;
        }

        // Visio patterns
        if (sku.Contains("VISIO"))
            return LicenseCategory.VisioOnline;

        // Project patterns
        if (sku.Contains("PROJECT"))
            return LicenseCategory.ProjectOnline;

        return LicenseCategory.Other;
    }

    /// <summary>
    /// Gets the license category for a given SKU ID (GUID).
    /// </summary>
    public static LicenseCategory GetCategoryBySkuId(string? skuId)
    {
        if (string.IsNullOrWhiteSpace(skuId))
            return LicenseCategory.Unknown;

        if (SkuIdMappings.TryGetValue(skuId, out var category))
            return category;

        return LicenseCategory.Unknown;
    }

    /// <summary>
    /// Gets the best license category based on both SKU Part Number and SKU ID.
    /// </summary>
    public static LicenseCategory GetBestCategory(string? skuPartNumber, string? skuId)
    {
        // Try SKU Part Number first (more reliable)
        var category = GetCategory(skuPartNumber);
        if (category != LicenseCategory.Unknown && category != LicenseCategory.Other)
            return category;

        // Fall back to SKU ID
        var idCategory = GetCategoryBySkuId(skuId);
        if (idCategory != LicenseCategory.Unknown)
            return idCategory;

        // Return whatever we got from part number (might be Other)
        return category;
    }

    /// <summary>
    /// Gets the list of features included in a license category.
    /// </summary>
    public static IReadOnlyList<string> GetIncludedFeatures(LicenseCategory category) => category switch
    {
        LicenseCategory.EnterpriseE5 => new[]
        {
            "MFA", "Conditional Access", "PIM", "Identity Protection",
            "Defender for Endpoint P2", "Defender for Office 365 P2",
            "Defender for Identity", "Defender for Cloud Apps",
            "Purview Information Protection", "eDiscovery Premium",
            "Insider Risk Management", "Communication Compliance",
            "Power BI Pro", "Audio Conferencing", "Teams Phone System"
        },
        LicenseCategory.EnterpriseE3 => new[]
        {
            "MFA", "Conditional Access", "Basic Purview",
            "eDiscovery Standard", "Litigation Hold",
            "Windows Enterprise"
        },
        LicenseCategory.EnterpriseE1 => new[]
        {
            "Basic MFA", "Exchange Online", "SharePoint", "Teams",
            "Web Apps Only"
        },
        LicenseCategory.BusinessPremium => new[]
        {
            "MFA", "Conditional Access", "Intune",
            "Defender for Business", "Azure AD P1",
            "Desktop Apps"
        },
        LicenseCategory.BusinessStandard => new[]
        {
            "Basic MFA", "Exchange Online", "SharePoint",
            "Teams", "Desktop Apps"
        },
        LicenseCategory.BusinessBasic => new[]
        {
            "Basic MFA", "Exchange Online", "SharePoint",
            "Teams", "Web Apps Only"
        },
        LicenseCategory.FrontlineF3 => new[]
        {
            "Limited Exchange", "Limited SharePoint",
            "Teams", "Basic Intune"
        },
        LicenseCategory.FrontlineF1 => new[]
        {
            "Teams", "Basic Apps", "Limited SharePoint"
        },
        LicenseCategory.EducationA5 => new[]
        {
            "MFA", "Conditional Access", "PIM",
            "Defender XDR", "Purview", "eDiscovery Premium",
            "Power BI Pro", "Audio Conferencing"
        },
        LicenseCategory.EducationA3 => new[]
        {
            "MFA", "Conditional Access",
            "Basic Purview", "eDiscovery Standard"
        },
        LicenseCategory.EducationA1 => new[]
        {
            "Basic MFA", "Exchange Online", "SharePoint",
            "Teams", "Web Apps Only"
        },
        LicenseCategory.GovernmentG5 => new[]
        {
            "MFA", "Conditional Access", "PIM",
            "Defender XDR (Gov)", "Purview", "eDiscovery Premium",
            "Power BI Pro", "Audio Conferencing"
        },
        LicenseCategory.GovernmentG3 => new[]
        {
            "MFA", "Conditional Access",
            "Basic Purview", "eDiscovery Standard"
        },
        LicenseCategory.GovernmentG1 => new[]
        {
            "Basic MFA", "Exchange Online", "SharePoint",
            "Teams", "Web Apps Only"
        },
        _ => Array.Empty<string>()
    };

    /// <summary>
    /// Gets the estimated monthly price per user for a license category (USD).
    /// </summary>
    public static decimal GetEstimatedMonthlyPrice(LicenseCategory category) => category switch
    {
        LicenseCategory.EnterpriseE5 => 57.00m,
        LicenseCategory.EnterpriseE3 => 36.00m,
        LicenseCategory.EnterpriseE1 => 10.00m,
        LicenseCategory.BusinessPremium => 22.00m,
        LicenseCategory.BusinessStandard => 12.50m,
        LicenseCategory.BusinessBasic => 6.00m,
        LicenseCategory.FrontlineF3 => 8.00m,
        LicenseCategory.FrontlineF1 => 2.25m,
        LicenseCategory.EducationA5 => 8.00m,  // Education pricing varies
        LicenseCategory.EducationA3 => 4.00m,
        LicenseCategory.EducationA1 => 0.00m,  // Often free for education
        LicenseCategory.DefenderForEndpointP1 => 3.00m,
        LicenseCategory.DefenderForEndpointP2 => 5.20m,
        LicenseCategory.DefenderForOffice365P1 => 2.00m,
        LicenseCategory.DefenderForOffice365P2 => 5.00m,
        LicenseCategory.AzureADPremiumP1 => 6.00m,
        LicenseCategory.AzureADPremiumP2 => 9.00m,
        LicenseCategory.IntuneP1 => 8.00m,
        LicenseCategory.PowerBIPro => 10.00m,
        LicenseCategory.PowerBIPremium => 20.00m,
        _ => 0.00m
    };

    /// <summary>
    /// Determines the primary license category for a user with multiple licenses.
    /// Returns the highest-tier primary license.
    /// </summary>
    public static LicenseCategory DeterminePrimaryLicense(IEnumerable<LicenseCategory> categories)
    {
        var primaryLicenses = categories
            .Where(c => c.IsPrimaryLicense())
            .OrderByDescending(c => c.GetTierRanking())
            .ToList();

        return primaryLicenses.FirstOrDefault();
    }

    /// <summary>
    /// Gets all unique categories from a list of SKU Part Numbers.
    /// </summary>
    public static List<LicenseCategory> GetCategories(IEnumerable<string> skuPartNumbers)
    {
        return skuPartNumbers
            .Select(GetCategory)
            .Where(c => c != LicenseCategory.Unknown)
            .Distinct()
            .ToList();
    }
}
