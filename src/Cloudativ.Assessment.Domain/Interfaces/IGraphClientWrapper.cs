namespace Cloudativ.Assessment.Domain.Interfaces;

public interface IGraphClientWrapper
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class;
    Task<List<T>> GetCollectionAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class;
    Task<string?> GetRawJsonAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetGrantedPermissionsAsync(CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);
}
