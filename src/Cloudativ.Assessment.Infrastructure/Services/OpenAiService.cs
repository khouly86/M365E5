using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiService> _logger;

    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    public OpenAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var enabled = _configuration.GetValue<bool>("OpenAI:Enabled");
        var apiKey = _configuration.GetValue<string>("OpenAI:ApiKey");
        return Task.FromResult(enabled && !string.IsNullOrWhiteSpace(apiKey));
    }

    public string GetModelName()
    {
        return _configuration.GetValue<string>("OpenAI:Model") ?? "gpt-4";
    }

    public async Task<OpenAiComplianceAnalysisResult> AnalyzeComplianceAsync(
        string assessmentFindingsJson,
        string? standardDocumentContent,
        ComplianceStandard standard,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration.GetValue<string>("OpenAI:ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new OpenAiComplianceAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "OpenAI API key is not configured"
                };
            }

            var model = GetModelName();
            var maxTokens = _configuration.GetValue<int>("OpenAI:MaxTokens", 4096);

            var hasDocument = !string.IsNullOrEmpty(standardDocumentContent);
            var systemPrompt = BuildSystemPrompt(standard, hasDocument);
            var userPrompt = BuildUserPrompt(assessmentFindingsJson, standardDocumentContent, standard, hasDocument);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = maxTokens,
                temperature = 0.1, // Low temperature for consistent, fact-based responses
                response_format = new { type = "json_object" }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, OpenAiApiUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            _logger.LogInformation("Sending compliance analysis request to OpenAI for standard: {Standard}", standard);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                return new OpenAiComplianceAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"OpenAI API error: {response.StatusCode}",
                    RawResponseJson = responseContent
                };
            }

            return ParseOpenAiResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API for compliance analysis");
            return new OpenAiComplianceAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Error calling OpenAI API: {ex.Message}"
            };
        }
    }

    private static string BuildSystemPrompt(ComplianceStandard standard, bool hasDocument)
    {
        var standardName = standard.GetDisplayName();
        var standardDescription = GetStandardDescription(standard);

        var documentInstructions = hasDocument
            ? @"CRITICAL RULES:
1. You MUST ONLY reference controls that are EXPLICITLY listed in the provided compliance standard document.
2. You MUST NOT make up, infer, or assume any control IDs or requirements that are not in the document.
3. When citing a control, use the EXACT control ID and name from the document.
4. If you cannot find a relevant control in the document for a finding, do not include it in the analysis."
            : $@"IMPORTANT: No external compliance document was provided. Use your comprehensive knowledge of {standardName}.

{standardDescription}

RULES:
1. Reference actual controls from the {standardName} framework using standard control IDs and names.
2. Use well-known control IDs and requirements that are publicly documented for this standard.
3. Focus on controls most relevant to Microsoft 365 and cloud security.
4. Be accurate in your control references - only cite controls that actually exist in the standard.";

        return $@"You are a compliance analyst specializing in {standardName} compliance for Microsoft 365 security configurations.

{documentInstructions}

GENERAL RULES:
5. When uncertain about compliance status, mark it as ""Partial"" rather than guessing.
6. Be conservative in your assessments - only mark something as ""Compliant"" if there is clear evidence.
7. Provide specific, actionable recommendations based on the assessment findings.

COMPLETENESS REQUIREMENTS (CRITICAL):
8. You MUST include ALL compliance gaps found - do not limit or truncate the list.
9. You MUST include ALL recommendations - provide a complete list, not a subset.
10. You MUST include ALL compliant areas - list every control that is compliant or partially compliant.
11. The counts (totalControls, compliantControls, etc.) MUST match the actual number of items in your arrays.
12. Do NOT arbitrarily limit any array to 10 or any other number - include the complete analysis.

OUTPUT FORMAT:
You must respond with a valid JSON object containing these exact fields:
{{
  ""complianceScore"": <number 0-100>,
  ""totalControls"": <number - must equal the total unique controls analyzed>,
  ""compliantControls"": <number - must equal compliantAreas array length>,
  ""partiallyCompliantControls"": <number - controls with partial compliance>,
  ""nonCompliantControls"": <number - must equal complianceGaps array length>,
  ""complianceGaps"": [
    // INCLUDE ALL GAPS - DO NOT LIMIT THIS LIST
    {{
      ""controlId"": ""<control ID>"",
      ""controlName"": ""<control name>"",
      ""gapDescription"": ""<specific gap description>"",
      ""severity"": ""Critical|High|Medium|Low"",
      ""currentState"": ""<what is currently configured>"",
      ""requiredState"": ""<what the standard requires>""
    }}
  ],
  ""recommendations"": [
    // INCLUDE ALL RECOMMENDATIONS - DO NOT LIMIT THIS LIST
    {{
      ""title"": ""<actionable title>"",
      ""priority"": ""Critical|High|Medium|Low"",
      ""implementationGuidance"": ""<specific steps to remediate>"",
      ""relatedControlIds"": [""<control IDs>""],
      ""estimatedEffort"": ""Quick Win|Short Term|Long Term""
    }}
  ],
  ""compliantAreas"": [
    // INCLUDE ALL COMPLIANT CONTROLS - DO NOT LIMIT THIS LIST
    {{
      ""controlId"": ""<control ID>"",
      ""controlName"": ""<control name>"",
      ""complianceStatus"": ""Compliant|Partial"",
      ""evidence"": ""<what assessment finding proves compliance>""
    }}
  ]
}}";
    }

    private static string GetStandardDescription(ComplianceStandard standard)
    {
        return standard switch
        {
            ComplianceStandard.NcaCcc => @"The Saudi National Cybersecurity Authority (NCA) Cloud Computing Controls (CCC) framework includes controls organized in domains such as:
- Cybersecurity Governance (CG)
- Cybersecurity Defense (CD)
- Cybersecurity Resilience (CR)
- Third Party Cybersecurity (TPC)
- Industrial Control Systems (ICS)
Key control areas include access management, data protection, logging and monitoring, incident response, and cloud-specific security requirements.",

            ComplianceStandard.Iso27001 => @"ISO/IEC 27001:2022 is an international standard for information security management systems (ISMS). Key control areas in Annex A include:
- A.5 Organizational controls (policies, roles, asset management)
- A.6 People controls (screening, awareness, disciplinary)
- A.7 Physical controls (security perimeters, equipment)
- A.8 Technological controls (access, cryptography, operations, network security)",

            ComplianceStandard.PciDss => @"PCI DSS v4.0 contains 12 principal requirements:
1. Install and maintain network security controls
2. Apply secure configurations
3. Protect stored account data
4. Protect cardholder data with strong cryptography
5. Protect against malware
6. Develop and maintain secure systems
7. Restrict access by business need
8. Identify users and authenticate access
9. Restrict physical access
10. Log and monitor all access
11. Test security regularly
12. Support information security with policies and programs",

            ComplianceStandard.Hipaa => @"HIPAA Security Rule includes three categories of safeguards:
- Administrative Safeguards (Security Management, Workforce Security, Information Access Management, Security Awareness, Incident Procedures, Contingency Plan)
- Physical Safeguards (Facility Access, Workstation Security, Device Controls)
- Technical Safeguards (Access Control, Audit Controls, Integrity, Authentication, Transmission Security)",

            ComplianceStandard.NistCsf => @"NIST Cybersecurity Framework 2.0 includes six core functions:
- Govern (GV): Organizational context, risk management strategy, roles and responsibilities
- Identify (ID): Asset management, business environment, governance, risk assessment
- Protect (PR): Access control, awareness training, data security, maintenance, protective technology
- Detect (DE): Anomalies, continuous monitoring, detection processes
- Respond (RS): Response planning, communications, analysis, mitigation, improvements
- Recover (RC): Recovery planning, improvements, communications",

            _ => $"The {standard.GetDisplayName()} compliance framework with its standard controls and requirements."
        };
    }

    private static string BuildUserPrompt(string assessmentFindingsJson, string? standardDocumentContent, ComplianceStandard standard, bool hasDocument)
    {
        var standardName = standard.GetDisplayName();

        if (hasDocument)
        {
            return $@"Analyze the following Microsoft 365 security assessment findings against the {standardName} compliance standard.

## COMPLIANCE STANDARD DOCUMENT:
{standardDocumentContent}

## ASSESSMENT FINDINGS:
{assessmentFindingsJson}

Based on the assessment findings and ONLY referencing controls explicitly listed in the compliance standard document above, provide a comprehensive compliance analysis.

Remember:
- Only cite controls that exist in the provided document
- Use exact control IDs and names from the document
- Be conservative when determining compliance status
- Provide specific, actionable recommendations
- Include evidence from the assessment findings for compliant areas
- IMPORTANT: Include ALL gaps, ALL recommendations, and ALL compliant areas - do not limit or truncate any lists";
        }
        else
        {
            return $@"Analyze the following Microsoft 365 security assessment findings against the {standardName} compliance standard.

## ASSESSMENT FINDINGS:
{assessmentFindingsJson}

Using your knowledge of the {standardName} framework and its controls, provide a comprehensive compliance analysis of the assessment findings.

Focus on:
1. Mapping findings to relevant {standardName} controls
2. Identifying compliance gaps where findings indicate non-compliance with standard requirements
3. Highlighting areas that demonstrate good compliance posture
4. Providing actionable recommendations for improving compliance

Remember:
- Use actual control IDs and names from the {standardName} standard
- Be conservative when determining compliance status
- Provide specific, actionable recommendations
- Include evidence from the assessment findings for compliant areas
- CRITICAL: Include ALL gaps, ALL recommendations, and ALL compliant areas - do not limit or truncate any lists to 10 or any arbitrary number";
        }
    }

    private OpenAiComplianceAnalysisResult ParseOpenAiResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // Extract usage information
            var promptTokens = 0;
            var completionTokens = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
                completionTokens = usage.GetProperty("completion_tokens").GetInt32();
            }

            // Extract the content from the first choice
            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(content))
            {
                return new OpenAiComplianceAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "Empty response from OpenAI",
                    RawResponseJson = responseJson,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens
                };
            }

            // Parse the content JSON
            using var contentDoc = JsonDocument.Parse(content);
            var contentRoot = contentDoc.RootElement;

            var complianceScore = contentRoot.GetProperty("complianceScore").GetInt32();
            var totalControls = contentRoot.GetProperty("totalControls").GetInt32();
            var compliantControls = contentRoot.GetProperty("compliantControls").GetInt32();
            var partiallyCompliantControls = contentRoot.GetProperty("partiallyCompliantControls").GetInt32();
            var nonCompliantControls = contentRoot.GetProperty("nonCompliantControls").GetInt32();

            var complianceGapsJson = contentRoot.TryGetProperty("complianceGaps", out var gaps)
                ? gaps.GetRawText()
                : "[]";

            var recommendationsJson = contentRoot.TryGetProperty("recommendations", out var recs)
                ? recs.GetRawText()
                : "[]";

            var compliantAreasJson = contentRoot.TryGetProperty("compliantAreas", out var areas)
                ? areas.GetRawText()
                : "[]";

            return new OpenAiComplianceAnalysisResult
            {
                Success = true,
                ComplianceScore = complianceScore,
                TotalControls = totalControls,
                CompliantControls = compliantControls,
                PartiallyCompliantControls = partiallyCompliantControls,
                NonCompliantControls = nonCompliantControls,
                ComplianceGapsJson = complianceGapsJson,
                RecommendationsJson = recommendationsJson,
                CompliantAreasJson = compliantAreasJson,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                RawResponseJson = responseJson
            };
        }
        catch (Exception ex)
        {
            return new OpenAiComplianceAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse OpenAI response: {ex.Message}",
                RawResponseJson = responseJson
            };
        }
    }
}
