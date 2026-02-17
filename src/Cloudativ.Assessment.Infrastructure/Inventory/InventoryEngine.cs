using System.Collections.Concurrent;
using Cloudativ.Assessment.Domain.Entities.Inventory;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Data;
using Cloudativ.Assessment.Infrastructure.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Inventory;

/// <summary>
/// Engine for orchestrating inventory collection across all modules.
/// </summary>
public class InventoryEngine : IInventoryEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventoryEngine> _logger;
    private readonly ConcurrentDictionary<Guid, InventoryProgress> _progressCache = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens = new();

    public event EventHandler<InventoryProgressEventArgs>? ProgressChanged;

    public InventoryEngine(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Guid> StartCollectionAsync(
        Guid tenantId,
        List<InventoryDomain>? domains,
        string? initiatedBy,
        CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create a master snapshot record
        var snapshotId = Guid.NewGuid();
        var domainsToCollect = domains ?? Enum.GetValues<InventoryDomain>().ToList();

        foreach (var domain in domainsToCollect)
        {
            var snapshot = new InventorySnapshot
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Domain = domain,
                InitiatedBy = initiatedBy,
                Status = InventoryStatus.Pending,
                CollectedAt = DateTime.UtcNow
            };
            dbContext.InventorySnapshots.Add(snapshot);
        }

        await dbContext.SaveChangesAsync(ct);

        // Initialize progress tracking
        var progress = new InventoryProgress
        {
            SnapshotId = snapshotId,
            Status = InventoryStatus.Pending,
            PendingDomains = domainsToCollect,
            StartedAt = DateTime.UtcNow
        };
        _progressCache[snapshotId] = progress;

        _logger.LogInformation(
            "Started inventory collection for tenant {TenantId}, snapshot {SnapshotId}, domains: {Domains}",
            tenantId, snapshotId, string.Join(", ", domainsToCollect));

        return snapshotId;
    }

    public async Task ExecuteCollectionAsync(Guid snapshotId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var graphClientFactory = scope.ServiceProvider.GetRequiredService<IGraphClientFactory>();
        var modules = scope.ServiceProvider.GetServices<IInventoryModule>().ToList();

        // Get snapshots for this collection
        var snapshots = await dbContext.InventorySnapshots
            .Where(s => s.Status == InventoryStatus.Pending)
            .OrderByDescending(s => s.CreatedAt)
            .Take(12)
            .ToListAsync(ct);

        if (!snapshots.Any())
        {
            _logger.LogWarning("No pending snapshots found for execution");
            return;
        }

        var tenantId = snapshots.First().TenantId;
        var tenant = await dbContext.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null)
        {
            _logger.LogError("Tenant {TenantId} not found", tenantId);
            return;
        }

        // Create cancellation token source
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _cancellationTokens[snapshotId] = cts;

        try
        {
            // Create Graph client for tenant
            var graphClient = await graphClientFactory.CreateClientAsync(tenant, cts.Token);
            if (graphClient == null)
            {
                _logger.LogError("Failed to create Graph client for tenant {TenantId}", tenantId);
                foreach (var snapshot in snapshots)
                {
                    snapshot.Status = InventoryStatus.Failed;
                    snapshot.ErrorMessage = "Failed to create Graph client";
                }
                await dbContext.SaveChangesAsync(cts.Token);
                return;
            }

            // Update progress to running
            if (_progressCache.TryGetValue(snapshotId, out var progress))
            {
                progress.Status = InventoryStatus.Running;
                OnProgressChanged(snapshotId, progress);
            }

            // Execute each module
            var startTime = DateTime.UtcNow;
            var totalItems = 0;
            var completedDomains = new List<InventoryDomain>();
            var failedDomains = new List<InventoryDomain>();

            foreach (var snapshot in snapshots)
            {
                if (cts.Token.IsCancellationRequested) break;

                var module = modules.FirstOrDefault(m => m.Domain == snapshot.Domain);
                if (module == null)
                {
                    _logger.LogWarning("No module found for domain {Domain}", snapshot.Domain);
                    snapshot.Status = InventoryStatus.Failed;
                    snapshot.ErrorMessage = "Module not found";
                    failedDomains.Add(snapshot.Domain);
                    continue;
                }

                // Update progress
                if (_progressCache.TryGetValue(snapshotId, out progress))
                {
                    progress.CurrentDomain = snapshot.Domain;
                    progress.ProgressPercentage = (double)completedDomains.Count / snapshots.Count * 100;
                    OnProgressChanged(snapshotId, progress);
                }

                snapshot.Status = InventoryStatus.Running;
                await dbContext.SaveChangesAsync(cts.Token);

                _logger.LogInformation(
                    "Collecting inventory for domain {Domain} in tenant {TenantId}",
                    snapshot.Domain, tenantId);

                try
                {
                    var moduleStartTime = DateTime.UtcNow;
                    var result = await module.CollectAsync(graphClient, tenantId, snapshot.Id, cts.Token);

                    snapshot.Status = result.Success ? InventoryStatus.Completed : InventoryStatus.PartiallyCompleted;
                    snapshot.ItemCount = result.ItemCount;
                    snapshot.Duration = result.Duration;
                    snapshot.ErrorMessage = result.ErrorMessage;
                    snapshot.WarningsJson = result.Warnings.Any()
                        ? System.Text.Json.JsonSerializer.Serialize(result.Warnings)
                        : null;

                    totalItems += result.ItemCount;

                    if (result.Success)
                    {
                        completedDomains.Add(snapshot.Domain);
                    }
                    else if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        failedDomains.Add(snapshot.Domain);
                    }

                    _logger.LogInformation(
                        "Completed inventory for domain {Domain}: {ItemCount} items in {Duration}",
                        snapshot.Domain, result.ItemCount, result.Duration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error collecting inventory for domain {Domain} in tenant {TenantId}",
                        snapshot.Domain, tenantId);

                    snapshot.Status = InventoryStatus.Failed;
                    snapshot.ErrorMessage = ex.Message;
                    snapshot.Duration = DateTime.UtcNow - startTime;
                    failedDomains.Add(snapshot.Domain);
                }

                await dbContext.SaveChangesAsync(cts.Token);
            }

            // Update final progress
            if (_progressCache.TryGetValue(snapshotId, out progress))
            {
                progress.Status = failedDomains.Any()
                    ? InventoryStatus.PartiallyCompleted
                    : InventoryStatus.Completed;
                progress.CompletedDomains = completedDomains;
                progress.FailedDomains = failedDomains;
                progress.TotalItemsCollected = totalItems;
                progress.CompletedAt = DateTime.UtcNow;
                progress.ProgressPercentage = 100;
                progress.CurrentDomain = null;
                OnProgressChanged(snapshotId, progress);
            }

            _logger.LogInformation(
                "Inventory collection completed for tenant {TenantId}: {TotalItems} items, " +
                "{CompletedCount} domains completed, {FailedCount} domains failed",
                tenantId, totalItems, completedDomains.Count, failedDomains.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Inventory collection cancelled for snapshot {SnapshotId}", snapshotId);

            foreach (var snapshot in snapshots.Where(s => s.Status == InventoryStatus.Running || s.Status == InventoryStatus.Pending))
            {
                snapshot.Status = InventoryStatus.Cancelled;
            }
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        finally
        {
            _cancellationTokens.TryRemove(snapshotId, out _);
        }
    }

    public Task<InventoryProgress> GetProgressAsync(Guid snapshotId, CancellationToken ct = default)
    {
        if (_progressCache.TryGetValue(snapshotId, out var progress))
        {
            return Task.FromResult(progress);
        }

        return Task.FromResult(new InventoryProgress
        {
            SnapshotId = snapshotId,
            Status = InventoryStatus.Pending
        });
    }

    public Task CancelCollectionAsync(Guid snapshotId, CancellationToken ct = default)
    {
        if (_cancellationTokens.TryGetValue(snapshotId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Cancellation requested for snapshot {SnapshotId}", snapshotId);
        }

        return Task.CompletedTask;
    }

    private void OnProgressChanged(Guid snapshotId, InventoryProgress progress)
    {
        ProgressChanged?.Invoke(this, new InventoryProgressEventArgs
        {
            SnapshotId = snapshotId,
            Progress = progress
        });
    }
}
