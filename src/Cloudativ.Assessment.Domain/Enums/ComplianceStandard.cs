namespace Cloudativ.Assessment.Domain.Enums;

/// <summary>
/// Compliance standards that can be assessed for M365 security configurations.
/// </summary>
public enum ComplianceStandard
{
    /// <summary>
    /// Saudi National Cybersecurity Authority Cloud Computing Controls (mandatory for Saudi tenants)
    /// </summary>
    NcaCcc = 1,

    /// <summary>
    /// ISO 27001:2022 Information Security Controls
    /// </summary>
    Iso27001 = 2,

    /// <summary>
    /// Payment Card Industry Data Security Standard v4.0 (Finance/Retail)
    /// </summary>
    PciDss = 3,

    /// <summary>
    /// Health Insurance Portability and Accountability Act Security Rule (Healthcare)
    /// </summary>
    Hipaa = 4,

    /// <summary>
    /// NIST Cybersecurity Framework (Government/General)
    /// </summary>
    NistCsf = 5
}

/// <summary>
/// Extension methods for ComplianceStandard enum.
/// </summary>
public static class ComplianceStandardExtensions
{
    /// <summary>
    /// Gets the display name for a compliance standard.
    /// </summary>
    public static string GetDisplayName(this ComplianceStandard standard) => standard switch
    {
        ComplianceStandard.NcaCcc => "Saudi NCA CCC",
        ComplianceStandard.Iso27001 => "ISO 27001:2022",
        ComplianceStandard.PciDss => "PCI DSS v4.0",
        ComplianceStandard.Hipaa => "HIPAA Security Rule",
        ComplianceStandard.NistCsf => "NIST CSF",
        _ => standard.ToString()
    };

    /// <summary>
    /// Gets the full description for a compliance standard.
    /// </summary>
    public static string GetDescription(this ComplianceStandard standard) => standard switch
    {
        ComplianceStandard.NcaCcc => "Saudi National Cybersecurity Authority Cloud Computing Controls - Mandatory for organizations operating in Saudi Arabia",
        ComplianceStandard.Iso27001 => "ISO 27001:2022 Information Security Management System Controls",
        ComplianceStandard.PciDss => "Payment Card Industry Data Security Standard v4.0 - Required for organizations handling payment card data",
        ComplianceStandard.Hipaa => "Health Insurance Portability and Accountability Act Security Rule - Required for healthcare organizations",
        ComplianceStandard.NistCsf => "NIST Cybersecurity Framework - Recommended for government and critical infrastructure organizations",
        _ => string.Empty
    };

    /// <summary>
    /// Gets the recommended compliance standards for a given industry.
    /// </summary>
    public static IReadOnlyList<ComplianceStandard> GetStandardsForIndustry(string? industry)
    {
        var standards = new List<ComplianceStandard>
        {
            // NCA CCC is mandatory for all (Saudi-focused application)
            ComplianceStandard.NcaCcc
        };

        switch (industry?.ToLowerInvariant())
        {
            case "finance":
                standards.Add(ComplianceStandard.PciDss);
                standards.Add(ComplianceStandard.Iso27001);
                break;

            case "retail":
                standards.Add(ComplianceStandard.PciDss);
                standards.Add(ComplianceStandard.Iso27001);
                break;

            case "healthcare":
                standards.Add(ComplianceStandard.Hipaa);
                standards.Add(ComplianceStandard.Iso27001);
                break;

            case "government":
                standards.Add(ComplianceStandard.NistCsf);
                standards.Add(ComplianceStandard.Iso27001);
                break;

            case "technology":
            case "manufacturing":
            case "education":
            case "other":
            default:
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.NistCsf);
                break;
        }

        return standards.AsReadOnly();
    }

    /// <summary>
    /// Gets all available compliance standards.
    /// </summary>
    public static IReadOnlyList<ComplianceStandard> GetAllStandards()
    {
        return Enum.GetValues<ComplianceStandard>().ToList().AsReadOnly();
    }

    /// <summary>
    /// Parses a comma-separated string of standards to a list of ComplianceStandard values.
    /// </summary>
    public static IReadOnlyList<ComplianceStandard> ParseStandards(string? standardsString)
    {
        if (string.IsNullOrWhiteSpace(standardsString))
            return Array.Empty<ComplianceStandard>();

        return standardsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Enum.TryParse<ComplianceStandard>(s, ignoreCase: true, out var standard) ? standard : (ComplianceStandard?)null)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Converts a list of ComplianceStandard values to a comma-separated string.
    /// </summary>
    public static string ToStandardsString(this IEnumerable<ComplianceStandard> standards)
    {
        return string.Join(",", standards.Distinct().Select(s => s.ToString()));
    }
}
