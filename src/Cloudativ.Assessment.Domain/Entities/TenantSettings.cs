namespace Cloudativ.Assessment.Domain.Entities;

public class TenantSettings : BaseEntity
{
    public Guid TenantId { get; set; }

    // Assessment settings
    public bool AutoScheduleEnabled { get; set; } = false;
    public string? ScheduleCron { get; set; }
    public string? EnabledDomainsJson { get; set; }

    // Notification settings
    public bool EmailNotificationsEnabled { get; set; } = false;
    public string? NotificationEmails { get; set; }

    // Report settings
    public bool IncludeRawDataInReports { get; set; } = false;
    public string? ReportLogoBase64 { get; set; }
    public string? CustomReportHeader { get; set; }

    // OpenAI settings
    public bool UseAiScoring { get; set; } = false;
    public string? AiModel { get; set; } = "gpt-4";

    // DKIM/DMARC/SPF manual inputs
    public string? ManualDkimConfig { get; set; }
    public string? ManualDmarcConfig { get; set; }
    public string? ManualSpfConfig { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}
