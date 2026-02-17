namespace Cloudativ.Assessment.Domain.Enums;

/// <summary>
/// Compliance standards that can be assessed for M365 security configurations.
/// </summary>
public enum ComplianceStandard
{
    // ── Saudi / Regional ────────────────────────────────────────────

    /// <summary>
    /// Saudi National Cybersecurity Authority Cloud Computing Controls (mandatory for Saudi tenants)
    /// </summary>
    NcaCcc = 1,

    /// <summary>
    /// Saudi National Cybersecurity Authority Essential Cybersecurity Controls
    /// </summary>
    NcaEcc = 30,

    /// <summary>
    /// Saudi National Cybersecurity Authority Data Cybersecurity Controls
    /// </summary>
    NcaDcc = 31,

    /// <summary>
    /// Saudi Arabia Personal Data Protection Law
    /// </summary>
    SaPdpl = 32,

    /// <summary>
    /// UAE Information Assurance Standards (IAS)
    /// </summary>
    UaeIas = 33,

    /// <summary>
    /// Qatar National Information Assurance Policy
    /// </summary>
    QatarNia = 34,

    // ── International ISO Standards ─────────────────────────────────

    /// <summary>
    /// ISO 27001:2022 Information Security Management System Controls
    /// </summary>
    Iso27001 = 2,

    /// <summary>
    /// ISO 27017:2015 Cloud Security Controls
    /// </summary>
    Iso27017 = 10,

    /// <summary>
    /// ISO 27018:2019 Protection of PII in Public Clouds
    /// </summary>
    Iso27018 = 11,

    /// <summary>
    /// ISO 27701:2019 Privacy Information Management (PIMS extension to ISO 27001)
    /// </summary>
    Iso27701 = 12,

    /// <summary>
    /// ISO 22301:2019 Business Continuity Management Systems
    /// </summary>
    Iso22301 = 13,

    // ── Payment / Finance ───────────────────────────────────────────

    /// <summary>
    /// Payment Card Industry Data Security Standard v4.0 (Finance/Retail)
    /// </summary>
    PciDss = 3,

    /// <summary>
    /// Sarbanes-Oxley Act (SOX) IT Controls (US publicly traded companies)
    /// </summary>
    Sox = 40,

    /// <summary>
    /// SWIFT Customer Security Programme (CSCF) for financial messaging
    /// </summary>
    SwiftCsp = 41,

    // ── Healthcare ──────────────────────────────────────────────────

    /// <summary>
    /// Health Insurance Portability and Accountability Act Security Rule (Healthcare)
    /// </summary>
    Hipaa = 4,

    /// <summary>
    /// HITRUST Common Security Framework
    /// </summary>
    Hitrust = 42,

    // ── NIST Family ─────────────────────────────────────────────────

    /// <summary>
    /// NIST Cybersecurity Framework (Government/General)
    /// </summary>
    NistCsf = 5,

    /// <summary>
    /// NIST SP 800-53 Rev 5 Security and Privacy Controls
    /// </summary>
    Nist80053 = 20,

    /// <summary>
    /// NIST SP 800-171 Rev 2 Controlled Unclassified Information (CUI)
    /// </summary>
    Nist800171 = 21,

    // ── US Government / Federal ─────────────────────────────────────

    /// <summary>
    /// FedRAMP (Federal Risk and Authorization Management Program)
    /// </summary>
    FedRamp = 22,

    /// <summary>
    /// CMMC 2.0 Cybersecurity Maturity Model Certification (US Defense supply chain)
    /// </summary>
    Cmmc = 23,

    /// <summary>
    /// CJIS Security Policy (Criminal Justice Information Services)
    /// </summary>
    Cjis = 24,

    /// <summary>
    /// ITAR International Traffic in Arms Regulations
    /// </summary>
    Itar = 25,

    // ── Privacy / Data Protection ───────────────────────────────────

    /// <summary>
    /// EU General Data Protection Regulation
    /// </summary>
    Gdpr = 50,

    /// <summary>
    /// California Consumer Privacy Act / California Privacy Rights Act
    /// </summary>
    Ccpa = 51,

    // ── Cloud / SaaS Audit ──────────────────────────────────────────

    /// <summary>
    /// SOC 2 Type II (Service Organization Controls – Trust Services Criteria)
    /// </summary>
    Soc2 = 60,

    /// <summary>
    /// SOC 1 Type II (SSAE 18 / ISAE 3402 – Financial reporting controls)
    /// </summary>
    Soc1 = 61,

    /// <summary>
    /// CSA STAR (Cloud Security Alliance Security Trust Assurance and Risk)
    /// </summary>
    CsaStar = 62,

    // ── Industry / Sector Specific ──────────────────────────────────

    /// <summary>
    /// CIS Controls v8 (Center for Internet Security Critical Security Controls)
    /// </summary>
    CisControls = 70,

    /// <summary>
    /// CIS Microsoft 365 Foundations Benchmark
    /// </summary>
    CisM365 = 71,

    /// <summary>
    /// COBIT 2019 (Control Objectives for Information and Related Technologies)
    /// </summary>
    Cobit = 72,

    /// <summary>
    /// NERC CIP (North American Electric Reliability Corporation Critical Infrastructure Protection)
    /// </summary>
    NercCip = 73,

    /// <summary>
    /// IEC 62443 Industrial Automation and Control Systems Security
    /// </summary>
    Iec62443 = 74,

    // ── Europe ──────────────────────────────────────────────────────

    /// <summary>
    /// EU NIS2 Directive (Network and Information Security)
    /// </summary>
    Nis2 = 80,

    /// <summary>
    /// EU DORA (Digital Operational Resilience Act – Financial sector)
    /// </summary>
    Dora = 81,

    /// <summary>
    /// UK Cyber Essentials Plus
    /// </summary>
    CyberEssentials = 82,

    /// <summary>
    /// BSI IT-Grundschutz (German Federal Office for Information Security)
    /// </summary>
    BsiGrundschutz = 83,

    // ── Asia-Pacific ────────────────────────────────────────────────

    /// <summary>
    /// Singapore MAS TRM (Monetary Authority of Singapore Technology Risk Management)
    /// </summary>
    MasTrm = 90,

    /// <summary>
    /// Australia IRAP (Information Security Registered Assessors Program)
    /// </summary>
    Irap = 91,

    /// <summary>
    /// India CERT-In Cybersecurity Directions
    /// </summary>
    CertIn = 92,

    // ── Microsoft Specific ──────────────────────────────────────────

    /// <summary>
    /// Microsoft 365 Secure Score Baseline
    /// </summary>
    M365SecureScore = 100,

    /// <summary>
    /// Microsoft Cloud Security Benchmark (MCSB)
    /// </summary>
    Mcsb = 101
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
        // Saudi / Regional
        ComplianceStandard.NcaCcc => "Saudi NCA CCC",
        ComplianceStandard.NcaEcc => "Saudi NCA ECC",
        ComplianceStandard.NcaDcc => "Saudi NCA DCC",
        ComplianceStandard.SaPdpl => "Saudi PDPL",
        ComplianceStandard.UaeIas => "UAE IAS",
        ComplianceStandard.QatarNia => "Qatar NIA",
        // ISO
        ComplianceStandard.Iso27001 => "ISO 27001:2022",
        ComplianceStandard.Iso27017 => "ISO 27017:2015",
        ComplianceStandard.Iso27018 => "ISO 27018:2019",
        ComplianceStandard.Iso27701 => "ISO 27701:2019",
        ComplianceStandard.Iso22301 => "ISO 22301:2019",
        // Payment / Finance
        ComplianceStandard.PciDss => "PCI DSS v4.0",
        ComplianceStandard.Sox => "SOX",
        ComplianceStandard.SwiftCsp => "SWIFT CSP",
        // Healthcare
        ComplianceStandard.Hipaa => "HIPAA Security Rule",
        ComplianceStandard.Hitrust => "HITRUST CSF",
        // NIST
        ComplianceStandard.NistCsf => "NIST CSF",
        ComplianceStandard.Nist80053 => "NIST SP 800-53",
        ComplianceStandard.Nist800171 => "NIST SP 800-171",
        // US Government
        ComplianceStandard.FedRamp => "FedRAMP",
        ComplianceStandard.Cmmc => "CMMC 2.0",
        ComplianceStandard.Cjis => "CJIS",
        ComplianceStandard.Itar => "ITAR",
        // Privacy
        ComplianceStandard.Gdpr => "GDPR",
        ComplianceStandard.Ccpa => "CCPA / CPRA",
        // Cloud Audit
        ComplianceStandard.Soc2 => "SOC 2 Type II",
        ComplianceStandard.Soc1 => "SOC 1 Type II",
        ComplianceStandard.CsaStar => "CSA STAR",
        // Industry
        ComplianceStandard.CisControls => "CIS Controls v8",
        ComplianceStandard.CisM365 => "CIS M365 Benchmark",
        ComplianceStandard.Cobit => "COBIT 2019",
        ComplianceStandard.NercCip => "NERC CIP",
        ComplianceStandard.Iec62443 => "IEC 62443",
        // Europe
        ComplianceStandard.Nis2 => "EU NIS2",
        ComplianceStandard.Dora => "EU DORA",
        ComplianceStandard.CyberEssentials => "UK Cyber Essentials",
        ComplianceStandard.BsiGrundschutz => "BSI IT-Grundschutz",
        // Asia-Pacific
        ComplianceStandard.MasTrm => "MAS TRM",
        ComplianceStandard.Irap => "Australia IRAP",
        ComplianceStandard.CertIn => "India CERT-In",
        // Microsoft
        ComplianceStandard.M365SecureScore => "M365 Secure Score",
        ComplianceStandard.Mcsb => "Microsoft CSB",
        _ => standard.ToString()
    };

    /// <summary>
    /// Gets the full description for a compliance standard.
    /// </summary>
    public static string GetDescription(this ComplianceStandard standard) => standard switch
    {
        // Saudi / Regional
        ComplianceStandard.NcaCcc => "Saudi National Cybersecurity Authority Cloud Computing Controls - Mandatory for organizations operating in Saudi Arabia",
        ComplianceStandard.NcaEcc => "Saudi NCA Essential Cybersecurity Controls - Baseline cybersecurity controls for all Saudi organizations",
        ComplianceStandard.NcaDcc => "Saudi NCA Data Cybersecurity Controls - Data protection controls for Saudi organizations",
        ComplianceStandard.SaPdpl => "Saudi Personal Data Protection Law - Privacy regulation for processing personal data in Saudi Arabia",
        ComplianceStandard.UaeIas => "UAE Information Assurance Standards - Cybersecurity framework for UAE government and critical entities",
        ComplianceStandard.QatarNia => "Qatar National Information Assurance Policy - Cybersecurity standards for Qatar organizations",
        // ISO
        ComplianceStandard.Iso27001 => "ISO 27001:2022 Information Security Management System Controls",
        ComplianceStandard.Iso27017 => "ISO 27017:2015 Code of Practice for Cloud Security - Cloud-specific information security controls",
        ComplianceStandard.Iso27018 => "ISO 27018:2019 Protection of Personally Identifiable Information in Public Clouds",
        ComplianceStandard.Iso27701 => "ISO 27701:2019 Privacy Information Management System - Extension to ISO 27001 for privacy",
        ComplianceStandard.Iso22301 => "ISO 22301:2019 Business Continuity Management Systems - Ensuring operational resilience",
        // Payment / Finance
        ComplianceStandard.PciDss => "Payment Card Industry Data Security Standard v4.0 - Required for organizations handling payment card data",
        ComplianceStandard.Sox => "Sarbanes-Oxley Act IT Controls - Required for US publicly traded companies",
        ComplianceStandard.SwiftCsp => "SWIFT Customer Security Programme - Mandatory security controls for SWIFT financial messaging users",
        // Healthcare
        ComplianceStandard.Hipaa => "Health Insurance Portability and Accountability Act Security Rule - Required for healthcare organizations",
        ComplianceStandard.Hitrust => "HITRUST Common Security Framework - Comprehensive security framework for healthcare and beyond",
        // NIST
        ComplianceStandard.NistCsf => "NIST Cybersecurity Framework - Recommended for government and critical infrastructure organizations",
        ComplianceStandard.Nist80053 => "NIST SP 800-53 Rev 5 - Comprehensive security and privacy controls for federal information systems",
        ComplianceStandard.Nist800171 => "NIST SP 800-171 Rev 2 - Protecting Controlled Unclassified Information in non-federal systems",
        // US Government
        ComplianceStandard.FedRamp => "Federal Risk and Authorization Management Program - Security authorization for cloud services used by US government",
        ComplianceStandard.Cmmc => "Cybersecurity Maturity Model Certification 2.0 - Required for US Department of Defense supply chain",
        ComplianceStandard.Cjis => "Criminal Justice Information Services Security Policy - Required for access to FBI CJIS data",
        ComplianceStandard.Itar => "International Traffic in Arms Regulations - Controls for defense-related articles and services",
        // Privacy
        ComplianceStandard.Gdpr => "EU General Data Protection Regulation - Privacy and data protection for EU residents' personal data",
        ComplianceStandard.Ccpa => "California Consumer Privacy Act / California Privacy Rights Act - Privacy rights for California residents",
        // Cloud Audit
        ComplianceStandard.Soc2 => "SOC 2 Type II - Trust Services Criteria audit for security, availability, processing integrity, confidentiality, and privacy",
        ComplianceStandard.Soc1 => "SOC 1 Type II (SSAE 18 / ISAE 3402) - Internal controls over financial reporting",
        ComplianceStandard.CsaStar => "Cloud Security Alliance STAR - Cloud security assurance and risk framework",
        // Industry
        ComplianceStandard.CisControls => "CIS Controls v8 - Prioritized set of actions to protect organizations from cyber attacks",
        ComplianceStandard.CisM365 => "CIS Microsoft 365 Foundations Benchmark - Security configuration best practices for M365",
        ComplianceStandard.Cobit => "COBIT 2019 - Governance and management framework for enterprise IT",
        ComplianceStandard.NercCip => "NERC CIP - Critical Infrastructure Protection standards for the North American electric grid",
        ComplianceStandard.Iec62443 => "IEC 62443 - Security for Industrial Automation and Control Systems (IACS/OT)",
        // Europe
        ComplianceStandard.Nis2 => "EU NIS2 Directive - Network and information security requirements for essential and important entities in the EU",
        ComplianceStandard.Dora => "EU Digital Operational Resilience Act - ICT risk management for financial entities in the EU",
        ComplianceStandard.CyberEssentials => "UK Cyber Essentials Plus - UK government-backed certification for baseline cybersecurity",
        ComplianceStandard.BsiGrundschutz => "BSI IT-Grundschutz - German Federal Office for Information Security baseline protection methodology",
        // Asia-Pacific
        ComplianceStandard.MasTrm => "MAS Technology Risk Management Guidelines - Cybersecurity standards for Singapore financial institutions",
        ComplianceStandard.Irap => "Australian Information Security Registered Assessors Program - Security assessment for Australian government cloud",
        ComplianceStandard.CertIn => "India CERT-In Cybersecurity Directions - Mandatory incident reporting and security measures for Indian organizations",
        // Microsoft
        ComplianceStandard.M365SecureScore => "Microsoft 365 Secure Score - Microsoft's built-in security posture measurement for M365 configurations",
        ComplianceStandard.Mcsb => "Microsoft Cloud Security Benchmark - Microsoft's recommended security best practices for Azure and M365",
        _ => string.Empty
    };

    /// <summary>
    /// Gets the recommended compliance standards for a given industry.
    /// </summary>
    public static IReadOnlyList<ComplianceStandard> GetStandardsForIndustry(string? industry)
    {
        var standards = new List<ComplianceStandard>
        {
            // NCA CCC & ECC are mandatory for all (Saudi-focused application)
            ComplianceStandard.NcaCcc,
            ComplianceStandard.NcaEcc
        };

        switch (industry?.ToLowerInvariant())
        {
            case "finance":
                standards.Add(ComplianceStandard.PciDss);
                standards.Add(ComplianceStandard.Sox);
                standards.Add(ComplianceStandard.SwiftCsp);
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.Soc2);
                standards.Add(ComplianceStandard.Dora);
                break;

            case "retail":
                standards.Add(ComplianceStandard.PciDss);
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.Gdpr);
                break;

            case "healthcare":
                standards.Add(ComplianceStandard.Hipaa);
                standards.Add(ComplianceStandard.Hitrust);
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.Gdpr);
                break;

            case "government":
                standards.Add(ComplianceStandard.NistCsf);
                standards.Add(ComplianceStandard.Nist80053);
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.CisControls);
                standards.Add(ComplianceStandard.CisM365);
                break;

            case "energy":
                standards.Add(ComplianceStandard.NercCip);
                standards.Add(ComplianceStandard.Iec62443);
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.NistCsf);
                break;

            case "defense":
                standards.Add(ComplianceStandard.Cmmc);
                standards.Add(ComplianceStandard.Nist800171);
                standards.Add(ComplianceStandard.Itar);
                standards.Add(ComplianceStandard.NistCsf);
                standards.Add(ComplianceStandard.Iso27001);
                break;

            case "technology":
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.Soc2);
                standards.Add(ComplianceStandard.CisControls);
                standards.Add(ComplianceStandard.Gdpr);
                standards.Add(ComplianceStandard.CsaStar);
                break;

            case "manufacturing":
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.Iec62443);
                standards.Add(ComplianceStandard.NistCsf);
                standards.Add(ComplianceStandard.CisControls);
                break;

            case "education":
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.NistCsf);
                standards.Add(ComplianceStandard.Gdpr);
                standards.Add(ComplianceStandard.CisM365);
                break;

            case "other":
            default:
                standards.Add(ComplianceStandard.Iso27001);
                standards.Add(ComplianceStandard.NistCsf);
                standards.Add(ComplianceStandard.CisControls);
                standards.Add(ComplianceStandard.M365SecureScore);
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
