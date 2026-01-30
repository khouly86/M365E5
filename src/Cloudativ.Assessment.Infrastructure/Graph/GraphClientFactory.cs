using Azure.Identity;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Cloudativ.Assessment.Infrastructure.Graph;

public interface IGraphClientFactory
{
    Task<IGraphClientWrapper> CreateClientAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task<IGraphClientWrapper> CreateClientAsync(string clientId, string clientSecret, string tenantId, CancellationToken cancellationToken = default);
    Task<IGraphClientWrapper> CreateDelegatedClientAsync(string tenantId, CancellationToken cancellationToken = default);
}

public class GraphClientFactory : IGraphClientFactory
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<GraphClientWrapper> _wrapperLogger;

    public GraphClientFactory(IEncryptionService encryptionService, ILogger<GraphClientWrapper> wrapperLogger)
    {
        _encryptionService = encryptionService;
        _wrapperLogger = wrapperLogger;
    }

    public async Task<IGraphClientWrapper> CreateClientAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenant.ClientId))
            throw new InvalidOperationException("Tenant does not have a configured client ID");

        if (string.IsNullOrEmpty(tenant.ClientSecretEncrypted))
            throw new InvalidOperationException("Tenant does not have configured credentials");

        if (!tenant.AzureTenantId.HasValue)
            throw new InvalidOperationException("Tenant does not have a configured Azure tenant ID");

        var clientSecret = _encryptionService.Decrypt(tenant.ClientSecretEncrypted);

        return await CreateClientAsync(
            tenant.ClientId,
            clientSecret,
            tenant.AzureTenantId.Value.ToString(),
            cancellationToken);
    }

    public Task<IGraphClientWrapper> CreateClientAsync(string clientId, string clientSecret, string tenantId, CancellationToken cancellationToken = default)
    {
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        };

        var credential = new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret,
            options);

        var graphClient = new GraphServiceClient(credential, scopes);

        return Task.FromResult<IGraphClientWrapper>(new GraphClientWrapper(graphClient, credential, _wrapperLogger));
    }

    public Task<IGraphClientWrapper> CreateDelegatedClientAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var scopes = new[]
        {
            "https://graph.microsoft.com/User.Read.All",
            "https://graph.microsoft.com/Directory.Read.All",
            "https://graph.microsoft.com/Organization.Read.All"
        };

        var options = new InteractiveBrowserCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            TenantId = tenantId,
            RedirectUri = new Uri("http://localhost")
        };

        var credential = new InteractiveBrowserCredential(options);

        var graphClient = new GraphServiceClient(credential, scopes);

        return Task.FromResult<IGraphClientWrapper>(new GraphClientWrapper(graphClient, credential, _wrapperLogger));
    }
}
