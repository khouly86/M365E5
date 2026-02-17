using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Graph;

public class GraphClientWrapper : IGraphClientWrapper
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphClientWrapper> _logger;
    private readonly TokenCredential? _credential;
    private List<string>? _cachedPermissions;

    public GraphClientWrapper(
        GraphServiceClient graphClient,
        TokenCredential? credential,
        ILogger<GraphClientWrapper> logger)
    {
        _graphClient = graphClient;
        _credential = credential;
        _logger = logger;
    }

    public GraphServiceClient Client => _graphClient;

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = await GetRawJsonAsync(endpoint, cancellationToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Graph endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<List<T>> GetCollectionAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class
    {
        var results = new List<T>();

        try
        {
            var json = await GetRawJsonAsync(endpoint, cancellationToken);
            if (string.IsNullOrEmpty(json))
                return results;

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    var obj = JsonSerializer.Deserialize<T>(item.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (obj != null)
                        results.Add(obj);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection from Graph endpoint: {Endpoint}", endpoint);
            throw;
        }

        return results;
    }

    public async Task<string?> GetRawJsonAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = endpoint.StartsWith("http")
                ? endpoint
                : $"https://graph.microsoft.com/v1.0/{endpoint}";

            using var httpClient = new HttpClient();
            var token = await GetAccessTokenAsync(cancellationToken);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Graph API returned {StatusCode} for {Endpoint}: {Error}",
                    response.StatusCode, endpoint, errorContent);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting raw JSON from Graph endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        // First, validate credentials by obtaining an access token
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
            return false;

        // Credentials are valid. Try Organization.GetAsync for org info,
        // but don't fail if it's just a missing permission (403).
        try
        {
            var org = await _graphClient.Organization.GetAsync(cancellationToken: cancellationToken);
            return org?.Value?.Any() == true;
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == 403)
        {
            _logger.LogInformation("Organization.Read.All permission not granted (403), but credentials are valid");
            return true;
        }
    }

    public async Task<List<string>> GetGrantedPermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedPermissions != null)
            return _cachedPermissions;

        var permissions = new List<string>();

        try
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var roles = jwtToken.Claims
                .Where(c => c.Type == "roles")
                .Select(c => c.Value)
                .ToList();

            permissions.AddRange(roles);
            _cachedPermissions = permissions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get granted permissions");
        }

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetGrantedPermissionsAsync(cancellationToken);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_credential == null)
            throw new InvalidOperationException("No credential configured");

        var context = new Azure.Core.TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
        var token = await _credential.GetTokenAsync(context, cancellationToken);
        return token.Token;
    }
}
