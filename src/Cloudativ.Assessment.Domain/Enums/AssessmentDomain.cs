namespace Cloudativ.Assessment.Domain.Enums;

public enum AssessmentDomain
{
    IdentityAndAccess = 1,
    PrivilegedAccess = 2,
    DeviceEndpoint = 3,
    ExchangeEmailSecurity = 4,
    MicrosoftDefender = 5,
    DataProtectionCompliance = 6,
    AuditLogging = 7,
    AppGovernance = 8,
    CollaborationSecurity = 9
}

public static class AssessmentDomainExtensions
{
    public static string GetDisplayName(this AssessmentDomain domain) => domain switch
    {
        AssessmentDomain.IdentityAndAccess => "Identity & Access (IAM)",
        AssessmentDomain.PrivilegedAccess => "Privileged Access / PIM",
        AssessmentDomain.DeviceEndpoint => "Device & Endpoint",
        AssessmentDomain.ExchangeEmailSecurity => "Exchange / Email Security",
        AssessmentDomain.MicrosoftDefender => "Microsoft Defender",
        AssessmentDomain.DataProtectionCompliance => "Data Protection & Compliance",
        AssessmentDomain.AuditLogging => "Audit & Logging",
        AssessmentDomain.AppGovernance => "App Governance / Consent",
        AssessmentDomain.CollaborationSecurity => "Collaboration Security",
        _ => domain.ToString()
    };

    public static string GetIconClass(this AssessmentDomain domain) => domain switch
    {
        AssessmentDomain.IdentityAndAccess => "fas fa-user-shield",
        AssessmentDomain.PrivilegedAccess => "fas fa-crown",
        AssessmentDomain.DeviceEndpoint => "fas fa-laptop",
        AssessmentDomain.ExchangeEmailSecurity => "fas fa-envelope-open-text",
        AssessmentDomain.MicrosoftDefender => "fas fa-shield-alt",
        AssessmentDomain.DataProtectionCompliance => "fas fa-lock",
        AssessmentDomain.AuditLogging => "fas fa-clipboard-list",
        AssessmentDomain.AppGovernance => "fas fa-puzzle-piece",
        AssessmentDomain.CollaborationSecurity => "fas fa-users",
        _ => "fas fa-circle"
    };
}
