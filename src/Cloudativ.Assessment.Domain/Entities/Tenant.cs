using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public Guid? AzureTenantId { get; set; }
    public OnboardingStatus OnboardingStatus { get; set; } = OnboardingStatus.Pending;
    public string? Industry { get; set; }
    public string? ContactEmail { get; set; }
    public string? Notes { get; set; }

    // Authentication type: "AppRegistration" or "PortalAdmin"
    public string AuthenticationType { get; set; } = "AppRegistration";

    // Azure AD App Registration credentials (encrypted in DB)
    public string? ClientId { get; set; }
    public string? SecretId { get; set; }
    public string? ClientSecretEncrypted { get; set; }

    // Optional OpenAI settings
    public bool OpenAiEnabled { get; set; } = false;
    public string? OpenAiApiKeyEncrypted { get; set; }

    // Governance - Selected compliance standards (comma-separated enum values)
    public string? SelectedComplianceStandards { get; set; }

    // Navigation properties
    public virtual ICollection<AssessmentRun> AssessmentRuns { get; set; } = new List<AssessmentRun>();
    public virtual ICollection<GovernanceAnalysis> GovernanceAnalyses { get; set; } = new List<GovernanceAnalysis>();
    public virtual TenantSettings? Settings { get; set; }
    public virtual Subscription? Subscription { get; set; }
}
