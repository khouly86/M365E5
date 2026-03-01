using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cloudativ.Assessment.Infrastructure.Services.Export;

public record StatCardData(string Label, string Value, string? SubText = null, string? Color = null);

/// <summary>
/// Reusable QuestPDF components for branded inventory PDF reports.
/// </summary>
public static class PdfReportComponents
{
    // Brand colors
    public const string PrimaryColor = "#E0FC8E";
    public const string SecondaryColor = "#51627A";
    public const string AccentColor = "#EF7348";
    public const string SuccessColor = "#16A34A";
    public const string WarningColor = "#CA8A04";
    public const string DangerColor = "#DC2626";
    public const string InfoColor = "#0078D4";
    public const string TextColor = "#374151";
    public const string LightTextColor = "#6B7280";
    public const string BorderColor = "#E5E7EB";

    // Chart palette for multi-segment charts
    public static readonly string[] ChartPalette = new[]
    {
        "#51627A", "#0078D4", "#038387", "#9C27B0", "#EF7348", "#E91E63",
        "#3F51B5", "#00897B", "#F57C00", "#7B1FA2"
    };

    /// <summary>
    /// Branded header with accent bar, title, tenant name, and timestamp.
    /// </summary>
    public static void ComposeHeader(IContainer container, string title, string tenantName, int totalItems)
    {
        container.Column(column =>
        {
            // Accent bar
            column.Item().Height(4).Background(SecondaryColor);

            column.Item().PaddingTop(8).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(title).FontSize(20).Bold().FontColor(SecondaryColor);
                    col.Item().PaddingTop(2).Text(tenantName).FontSize(11).FontColor(LightTextColor);
                });
                row.ConstantItem(180).Column(col =>
                {
                    col.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(8).FontColor(LightTextColor);
                    col.Item().AlignRight().PaddingTop(2).Text($"Total Items: {totalItems:N0}")
                        .FontSize(10).Bold().FontColor(SecondaryColor);
                });
            });

            column.Item().LineHorizontal(1).LineColor(BorderColor);
        });
    }

    /// <summary>
    /// Row of stat cards showing key metrics.
    /// </summary>
    public static void ComposeStatCards(IContainer container, List<StatCardData> cards)
    {
        container.Row(row =>
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (i > 0) row.ConstantItem(10); // gap

                var card = cards[i];
                var valueColor = card.Color ?? SecondaryColor;

                row.RelativeItem().Border(1).BorderColor(BorderColor)
                    .Background(Colors.White).Padding(10).Column(col =>
                {
                    col.Item().Text(card.Value).FontSize(22).Bold().FontColor(valueColor);
                    col.Item().PaddingTop(2).Text(card.Label).FontSize(9).FontColor(LightTextColor);
                    if (!string.IsNullOrEmpty(card.SubText))
                    {
                        col.Item().PaddingTop(1).Text(card.SubText).FontSize(8).FontColor(LightTextColor);
                    }
                });
            }
        });
    }

    /// <summary>
    /// Section title with horizontal line below.
    /// </summary>
    public static void SectionTitle(IContainer container, string title)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(13).Bold().FontColor(SecondaryColor);
            col.Item().PaddingTop(3).LineHorizontal(1).LineColor(BorderColor);
        });
    }

    /// <summary>
    /// Data table with header row, alternating row colors, and status badge support.
    /// </summary>
    public static void ComposeDataTable(IContainer container, string[] headers,
        List<string[]> rows, int[]? badgeColumns = null)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                    columns.RelativeColumn();
            });

            // Header row
            table.Header(header =>
            {
                foreach (var h in headers)
                {
                    header.Cell().Background(SecondaryColor).Padding(5)
                        .Text(h).FontColor(Colors.White).FontSize(8).Bold();
                }
            });

            // Data rows
            bool alternate = false;
            var badgeSet = new HashSet<int>(badgeColumns ?? Array.Empty<int>());

            foreach (var row in rows)
            {
                var bgColor = alternate ? "#F9FAFB" : "#FFFFFF";
                for (int i = 0; i < row.Length; i++)
                {
                    var cellValue = row[i];
                    var cell = table.Cell().Background(bgColor).Padding(4);

                    if (badgeSet.Contains(i))
                    {
                        cell.Element(c => StatusBadge(c, cellValue));
                    }
                    else
                    {
                        cell.Text(cellValue).FontSize(8);
                    }
                }
                alternate = !alternate;
            }
        });
    }

    /// <summary>
    /// Colored status badge for table cells.
    /// </summary>
    public static void StatusBadge(IContainer container, string text)
    {
        var (bgColor, textColor) = GetBadgeColors(text);

        container.AlignLeft()
            .Background(bgColor)
            .PaddingHorizontal(6).PaddingVertical(2)
            .Text(text).FontSize(7).Bold().FontColor(textColor);
    }

    /// <summary>
    /// Branded footer with page numbers.
    /// </summary>
    public static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(BorderColor);
            col.Item().PaddingTop(5).Row(footerRow =>
            {
                footerRow.RelativeItem().Text("Cloudativ Assessment Tool")
                    .FontSize(8).FontColor(LightTextColor);
                footerRow.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(LightTextColor);
                    text.CurrentPageNumber().FontSize(8).FontColor(TextColor);
                    text.Span(" of ").FontSize(8).FontColor(LightTextColor);
                    text.TotalPages().FontSize(8).FontColor(TextColor);
                });
            });
        });
    }

    /// <summary>
    /// Two-column chart layout wrapper.
    /// </summary>
    public static void ChartRow(IContainer container,
        Action<IContainer> leftChart, Action<IContainer> rightChart)
    {
        container.Row(row =>
        {
            row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(10)
                .Element(leftChart);

            row.ConstantItem(12); // gap

            row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(10)
                .Element(rightChart);
        });
    }

    private static (string bg, string text) GetBadgeColors(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "yes" or "active" or "enabled" or "compliant" or "true" or "none"
                => ("#DCFCE7", "#166534"),
            "no" or "disabled" or "noncompliant" or "non-compliant" or "false"
                => ("#FEE2E2", "#991B1B"),
            "high" or "critical"
                => ("#FEE2E2", "#991B1B"),
            "medium" or "warning"
                => ("#FEF3C7", "#92400E"),
            "low" or "report-only" or "enabledforreportingbutnotenforced"
                => ("#DBEAFE", "#1E40AF"),
            _ => ("#F3F4F6", "#374151")
        };
    }
}
