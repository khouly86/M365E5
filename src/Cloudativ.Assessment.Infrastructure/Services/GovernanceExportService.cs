using ClosedXML.Excel;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class GovernanceExportService : IGovernanceExportService
{
    private readonly IGovernanceService _governanceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GovernanceExportService> _logger;

    // Cloudativ brand colors
    private const string PrimaryColor = "#E0FC8E";
    private const string SecondaryColor = "#51627A";
    private const string AccentColor = "#EF7348";

    static GovernanceExportService()
    {
        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public GovernanceExportService(
        IGovernanceService governanceService,
        IUnitOfWork unitOfWork,
        ILogger<GovernanceExportService> logger)
    {
        _governanceService = governanceService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> ExportToPdfAsync(
        Guid assessmentRunId,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting governance analysis to PDF for run {RunId}", assessmentRunId);

        var analyses = await _governanceService.GetAnalysesByRunAsync(assessmentRunId, cancellationToken);
        var summary = await _governanceService.GetAnalysisSummaryAsync(assessmentRunId, cancellationToken);

        if (!analyses.Any())
        {
            throw new InvalidOperationException("No governance analyses found for this assessment run.");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, tenantName, summary));
                page.Content().Element(c => ComposeContent(c, analyses.ToList(), summary));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportToExcelAsync(
        Guid assessmentRunId,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting governance analysis to Excel for run {RunId}", assessmentRunId);

        var analyses = await _governanceService.GetAnalysesByRunAsync(assessmentRunId, cancellationToken);
        var summary = await _governanceService.GetAnalysisSummaryAsync(assessmentRunId, cancellationToken);

        if (!analyses.Any())
        {
            throw new InvalidOperationException("No governance analyses found for this assessment run.");
        }

        using var workbook = new XLWorkbook();

        // Summary Sheet
        var summarySheet = workbook.Worksheets.Add("Summary");
        CreateSummarySheet(summarySheet, tenantName, summary, analyses.ToList());

        // Individual sheets for each standard
        foreach (var analysis in analyses.Where(a => a.IsSuccessful))
        {
            var sheetName = SanitizeSheetName(analysis.StandardDisplayName);
            var sheet = workbook.Worksheets.Add(sheetName);
            CreateAnalysisSheet(sheet, analysis);
        }

        // Gaps Sheet
        var gapsSheet = workbook.Worksheets.Add("All Compliance Gaps");
        CreateGapsSheet(gapsSheet, analyses.ToList());

        // Recommendations Sheet
        var recsSheet = workbook.Worksheets.Add("All Recommendations");
        CreateRecommendationsSheet(recsSheet, analyses.ToList());

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportSingleAnalysisToPdfAsync(
        Guid analysisId,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        var analysis = await _governanceService.GetAnalysisAsync(analysisId, cancellationToken);
        if (analysis == null)
        {
            throw new InvalidOperationException("Governance analysis not found.");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeSingleHeader(c, tenantName, analysis));
                page.Content().Element(c => ComposeSingleContent(c, analysis));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    #region PDF Composition Methods

    private void ComposeHeader(IContainer container, string tenantName, GovernanceAnalysisSummaryDto? summary)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Cloudativ Assessment")
                        .FontSize(24).Bold().FontColor(SecondaryColor);
                    col.Item().Text("Governance Compliance Report")
                        .FontSize(14).FontColor(SecondaryColor);
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text($"Tenant: {tenantName}").FontSize(10);
                    col.Item().Text($"Generated: {DateTime.Now:MMM dd, yyyy HH:mm}").FontSize(10);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(2).LineColor(PrimaryColor);

            if (summary != null)
            {
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                    {
                        col.Item().Text("Overall Score").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"{summary.OverallAverageScore}%").FontSize(24).Bold()
                            .FontColor(GetScoreColor(summary.OverallAverageScore));
                    });

                    row.ConstantItem(10);

                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                    {
                        col.Item().Text("Standards Analyzed").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"{summary.StandardsAnalyzed}").FontSize(24).Bold().FontColor(SecondaryColor);
                    });

                    row.ConstantItem(10);

                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                    {
                        col.Item().Text("Compliance Gaps").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"{summary.TotalGaps}").FontSize(24).Bold().FontColor(AccentColor);
                    });

                    row.ConstantItem(10);

                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                    {
                        col.Item().Text("Recommendations").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"{summary.TotalRecommendations}").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                    });
                });
            }

            column.Item().PaddingVertical(15);
        });
    }

    private void ComposeContent(IContainer container, List<GovernanceAnalysisDto> analyses, GovernanceAnalysisSummaryDto? summary)
    {
        container.Column(column =>
        {
            foreach (var analysis in analyses.Where(a => a.IsSuccessful))
            {
                column.Item().Element(c => ComposeAnalysisSection(c, analysis));
                column.Item().PaddingVertical(10);
            }
        });
    }

    private void ComposeAnalysisSection(IContainer container, GovernanceAnalysisDto analysis)
    {
        container.Column(column =>
        {
            // Standard header
            column.Item().Background(SecondaryColor).Padding(10).Row(row =>
            {
                row.RelativeItem().Text(analysis.StandardDisplayName)
                    .FontSize(14).Bold().FontColor(Colors.White);
                row.ConstantItem(80).AlignRight().Text($"Score: {analysis.ComplianceScore}%")
                    .FontSize(14).Bold().FontColor(PrimaryColor);
            });

            // Control counts
            column.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                row.RelativeItem().Text($"Total Controls: {analysis.TotalControls}").FontSize(9);
                row.RelativeItem().Text($"Compliant: {analysis.CompliantControls}").FontSize(9).FontColor(Colors.Green.Darken2);
                row.RelativeItem().Text($"Partial: {analysis.PartiallyCompliantControls}").FontSize(9).FontColor(Colors.Orange.Darken2);
                row.RelativeItem().Text($"Non-Compliant: {analysis.NonCompliantControls}").FontSize(9).FontColor(Colors.Red.Darken2);
            });

            // Gaps
            if (analysis.ComplianceGaps?.Any() == true)
            {
                column.Item().PaddingTop(10).Text($"Compliance Gaps ({analysis.ComplianceGaps.Count})").FontSize(11).Bold().FontColor(AccentColor);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(80);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(3);
                        cols.ConstantColumn(60);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Control ID").FontSize(8).Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Control Name").FontSize(8).Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Gap Description").FontSize(8).Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Severity").FontSize(8).Bold();
                    });

                    foreach (var gap in analysis.ComplianceGaps)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(gap.ControlId ?? "-").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(gap.ControlName ?? "-").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(gap.GapDescription ?? "-").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(gap.Severity ?? "-").FontSize(8)
                            .FontColor(GetSeverityColor(gap.Severity));
                    }
                });
            }

            // Recommendations
            if (analysis.Recommendations?.Any() == true)
            {
                column.Item().PaddingTop(10).Text($"Recommendations ({analysis.Recommendations.Count})").FontSize(11).Bold().FontColor(Colors.Blue.Darken2);

                foreach (var rec in analysis.Recommendations)
                {
                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(recCol =>
                    {
                        recCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text(rec.Title ?? "Recommendation").FontSize(9).Bold();
                            row.ConstantItem(80).AlignRight()
                                .Text(rec.Priority ?? "-").FontSize(8)
                                .FontColor(GetPriorityColor(rec.Priority));
                        });
                        if (!string.IsNullOrEmpty(rec.ImplementationGuidance))
                        {
                            recCol.Item().PaddingTop(4).Text(rec.ImplementationGuidance).FontSize(8);
                        }
                    });
                }
            }

            // Compliant Areas
            if (analysis.CompliantAreas?.Any() == true)
            {
                column.Item().PaddingTop(10).Text($"Compliant Areas ({analysis.CompliantAreas.Count})").FontSize(11).Bold().FontColor(Colors.Green.Darken2);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(80);
                        cols.RelativeColumn(2);
                        cols.ConstantColumn(60);
                        cols.RelativeColumn(3);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Control ID").FontSize(8).Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Control Name").FontSize(8).Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Status").FontSize(8).Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Evidence").FontSize(8).Bold();
                    });

                    foreach (var area in analysis.CompliantAreas)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(area.ControlId ?? "-").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(area.ControlName ?? "-").FontSize(8);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(area.ComplianceStatus ?? "-").FontSize(8)
                            .FontColor(Colors.Green.Darken2);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                            .Text(area.Evidence ?? "-").FontSize(8);
                    }
                });
            }
        });
    }

    private void ComposeSingleHeader(IContainer container, string tenantName, GovernanceAnalysisDto analysis)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Cloudativ Assessment")
                        .FontSize(24).Bold().FontColor(SecondaryColor);
                    col.Item().Text($"{analysis.StandardDisplayName} Compliance Report")
                        .FontSize(14).FontColor(SecondaryColor);
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text($"Tenant: {tenantName}").FontSize(10);
                    col.Item().Text($"Generated: {DateTime.Now:MMM dd, yyyy HH:mm}").FontSize(10);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(2).LineColor(PrimaryColor);
            column.Item().PaddingVertical(15);
        });
    }

    private void ComposeSingleContent(IContainer container, GovernanceAnalysisDto analysis)
    {
        container.Element(c => ComposeAnalysisSection(c, analysis));
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
            text.Span("  |  ");
            text.Span("Generated by Cloudativ Assessment").FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static string GetScoreColor(int score) => score switch
    {
        >= 80 => Colors.Green.Darken2,
        >= 60 => Colors.Orange.Darken2,
        _ => Colors.Red.Darken2
    };

    private static string GetSeverityColor(string? severity) => severity?.ToLower() switch
    {
        "critical" => Colors.Red.Darken3,
        "high" => Colors.Red.Darken1,
        "medium" => Colors.Orange.Darken2,
        "low" => Colors.Blue.Darken1,
        _ => Colors.Grey.Darken1
    };

    private static string GetPriorityColor(string? priority) => priority?.ToLower() switch
    {
        "critical" => Colors.Red.Darken3,
        "high" => Colors.Red.Darken1,
        "medium" => Colors.Orange.Darken2,
        "low" => Colors.Blue.Darken1,
        _ => Colors.Grey.Darken1
    };

    #endregion

    #region Excel Composition Methods

    private void CreateSummarySheet(IXLWorksheet sheet, string tenantName, GovernanceAnalysisSummaryDto? summary, List<GovernanceAnalysisDto> analyses)
    {
        var row = 1;

        // Title
        sheet.Cell(row, 1).Value = "Cloudativ Assessment - Governance Compliance Report";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 16;
        sheet.Range(row, 1, row, 4).Merge();
        row += 2;

        // Tenant info
        sheet.Cell(row, 1).Value = "Tenant:";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = tenantName;
        row++;

        sheet.Cell(row, 1).Value = "Generated:";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
        row += 2;

        if (summary != null)
        {
            // Summary section
            sheet.Cell(row, 1).Value = "Summary";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            sheet.Cell(row, 1).Value = "Overall Score:";
            sheet.Cell(row, 2).Value = $"{summary.OverallAverageScore}%";
            sheet.Cell(row, 2).Style.Font.Bold = true;
            row++;

            sheet.Cell(row, 1).Value = "Standards Analyzed:";
            sheet.Cell(row, 2).Value = summary.StandardsAnalyzed;
            row++;

            sheet.Cell(row, 1).Value = "Total Gaps:";
            sheet.Cell(row, 2).Value = summary.TotalGaps;
            row++;

            sheet.Cell(row, 1).Value = "Total Recommendations:";
            sheet.Cell(row, 2).Value = summary.TotalRecommendations;
            row += 2;
        }

        // Standards table
        sheet.Cell(row, 1).Value = "Standard";
        sheet.Cell(row, 2).Value = "Score";
        sheet.Cell(row, 3).Value = "Compliant";
        sheet.Cell(row, 4).Value = "Partial";
        sheet.Cell(row, 5).Value = "Non-Compliant";
        sheet.Cell(row, 6).Value = "Gaps";
        var headerRange = sheet.Range(row, 1, row, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
        headerRange.Style.Font.FontColor = XLColor.White;
        row++;

        foreach (var analysis in analyses)
        {
            sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
            sheet.Cell(row, 2).Value = $"{analysis.ComplianceScore}%";
            sheet.Cell(row, 3).Value = analysis.CompliantControls;
            sheet.Cell(row, 4).Value = analysis.PartiallyCompliantControls;
            sheet.Cell(row, 5).Value = analysis.NonCompliantControls;
            sheet.Cell(row, 6).Value = analysis.ComplianceGaps?.Count ?? 0;

            // Color code score
            if (analysis.ComplianceScore >= 80)
                sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
            else if (analysis.ComplianceScore >= 60)
                sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Orange;
            else
                sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;

            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private void CreateAnalysisSheet(IXLWorksheet sheet, GovernanceAnalysisDto analysis)
    {
        var row = 1;

        // Header
        sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Summary
        sheet.Cell(row, 1).Value = "Compliance Score:";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = $"{analysis.ComplianceScore}%";
        row++;

        sheet.Cell(row, 1).Value = "Total Controls:";
        sheet.Cell(row, 2).Value = analysis.TotalControls;
        row++;

        sheet.Cell(row, 1).Value = "Compliant:";
        sheet.Cell(row, 2).Value = analysis.CompliantControls;
        row++;

        sheet.Cell(row, 1).Value = "Partially Compliant:";
        sheet.Cell(row, 2).Value = analysis.PartiallyCompliantControls;
        row++;

        sheet.Cell(row, 1).Value = "Non-Compliant:";
        sheet.Cell(row, 2).Value = analysis.NonCompliantControls;
        row += 2;

        // Gaps
        if (analysis.ComplianceGaps?.Any() == true)
        {
            sheet.Cell(row, 1).Value = "Compliance Gaps";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            sheet.Cell(row, 1).Value = "Control ID";
            sheet.Cell(row, 2).Value = "Control Name";
            sheet.Cell(row, 3).Value = "Gap Description";
            sheet.Cell(row, 4).Value = "Severity";
            sheet.Cell(row, 5).Value = "Current State";
            sheet.Cell(row, 6).Value = "Required State";
            var gapHeader = sheet.Range(row, 1, row, 6);
            gapHeader.Style.Font.Bold = true;
            gapHeader.Style.Fill.BackgroundColor = XLColor.FromHtml(AccentColor);
            gapHeader.Style.Font.FontColor = XLColor.White;
            row++;

            foreach (var gap in analysis.ComplianceGaps)
            {
                sheet.Cell(row, 1).Value = gap.ControlId ?? "-";
                sheet.Cell(row, 2).Value = gap.ControlName ?? "-";
                sheet.Cell(row, 3).Value = gap.GapDescription ?? "-";
                sheet.Cell(row, 4).Value = gap.Severity ?? "-";
                sheet.Cell(row, 5).Value = gap.CurrentState ?? "-";
                sheet.Cell(row, 6).Value = gap.RequiredState ?? "-";
                row++;
            }
            row++;
        }

        // Recommendations
        if (analysis.Recommendations?.Any() == true)
        {
            sheet.Cell(row, 1).Value = "Recommendations";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            sheet.Cell(row, 1).Value = "Title";
            sheet.Cell(row, 2).Value = "Priority";
            sheet.Cell(row, 3).Value = "Implementation Guidance";
            sheet.Cell(row, 4).Value = "Estimated Effort";
            var recHeader = sheet.Range(row, 1, row, 4);
            recHeader.Style.Font.Bold = true;
            recHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976d2");
            recHeader.Style.Font.FontColor = XLColor.White;
            row++;

            foreach (var rec in analysis.Recommendations)
            {
                sheet.Cell(row, 1).Value = rec.Title ?? "-";
                sheet.Cell(row, 2).Value = rec.Priority ?? "-";
                sheet.Cell(row, 3).Value = rec.ImplementationGuidance ?? "-";
                sheet.Cell(row, 4).Value = rec.EstimatedEffort ?? "-";
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(3).Width = 60; // Wider for descriptions
    }

    private void CreateGapsSheet(IXLWorksheet sheet, List<GovernanceAnalysisDto> analyses)
    {
        var row = 1;

        sheet.Cell(row, 1).Value = "Standard";
        sheet.Cell(row, 2).Value = "Control ID";
        sheet.Cell(row, 3).Value = "Control Name";
        sheet.Cell(row, 4).Value = "Gap Description";
        sheet.Cell(row, 5).Value = "Severity";
        sheet.Cell(row, 6).Value = "Current State";
        sheet.Cell(row, 7).Value = "Required State";
        var header = sheet.Range(row, 1, row, 7);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(AccentColor);
        header.Style.Font.FontColor = XLColor.White;
        row++;

        foreach (var analysis in analyses.Where(a => a.IsSuccessful && a.ComplianceGaps?.Any() == true))
        {
            foreach (var gap in analysis.ComplianceGaps!)
            {
                sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
                sheet.Cell(row, 2).Value = gap.ControlId ?? "-";
                sheet.Cell(row, 3).Value = gap.ControlName ?? "-";
                sheet.Cell(row, 4).Value = gap.GapDescription ?? "-";
                sheet.Cell(row, 5).Value = gap.Severity ?? "-";
                sheet.Cell(row, 6).Value = gap.CurrentState ?? "-";
                sheet.Cell(row, 7).Value = gap.RequiredState ?? "-";
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(4).Width = 60;
    }

    private void CreateRecommendationsSheet(IXLWorksheet sheet, List<GovernanceAnalysisDto> analyses)
    {
        var row = 1;

        sheet.Cell(row, 1).Value = "Standard";
        sheet.Cell(row, 2).Value = "Title";
        sheet.Cell(row, 3).Value = "Priority";
        sheet.Cell(row, 4).Value = "Implementation Guidance";
        sheet.Cell(row, 5).Value = "Estimated Effort";
        var header = sheet.Range(row, 1, row, 5);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976d2");
        header.Style.Font.FontColor = XLColor.White;
        row++;

        foreach (var analysis in analyses.Where(a => a.IsSuccessful && a.Recommendations?.Any() == true))
        {
            foreach (var rec in analysis.Recommendations!)
            {
                sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
                sheet.Cell(row, 2).Value = rec.Title ?? "-";
                sheet.Cell(row, 3).Value = rec.Priority ?? "-";
                sheet.Cell(row, 4).Value = rec.ImplementationGuidance ?? "-";
                sheet.Cell(row, 5).Value = rec.EstimatedEffort ?? "-";
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(4).Width = 60;
    }

    /// <summary>
    /// Sanitizes a string to be valid as an Excel sheet name.
    /// Excel sheet names cannot contain: : \ / ? * [ ]
    /// Maximum length is 31 characters.
    /// </summary>
    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Sheet";

        // Replace invalid characters
        var sanitized = name
            .Replace(":", "-")
            .Replace("\\", "-")
            .Replace("/", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("[", "(")
            .Replace("]", ")");

        // Trim to max 31 characters
        if (sanitized.Length > 31)
            sanitized = sanitized.Substring(0, 31);

        return sanitized;
    }

    #endregion
}
