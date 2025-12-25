using Microsoft.EntityFrameworkCore;
using CloudWatcher.Data;
using CloudWatcher.Models;

namespace CloudWatcher.Services
{
    /// <summary>
    /// Background service for managing inventory audit log retention.
    /// Periodically purges old audit logs based on configured retention period.
    /// </summary>
    public class InventoryAuditRetentionService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InventoryAuditRetentionService> _logger;
        private readonly IConfiguration _configuration;

        public InventoryAuditRetentionService(
            IServiceProvider serviceProvider,
            ILogger<InventoryAuditRetentionService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("InventoryAuditRetentionService started");

            // Wait 1 minute before first run to allow app to settle
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Run daily at configured time (default: every 24 hours from startup)
                    var runInterval = _configuration.GetValue<int>("InventoryAuditRetention:RunIntervalHours", 24);
                    
                    _logger.LogInformation("Running inventory audit retention purge");
                    await PurgeAuditLogs(stoppingToken);

                    // Wait for next run
                    await Task.Delay(TimeSpan.FromHours(runInterval), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("InventoryAuditRetentionService cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in InventoryAuditRetentionService");
                    // Continue on error, retry after interval
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("InventoryAuditRetentionService stopped");
        }

        /// <summary>
        /// Execute the audit log purge. Can be called directly for manual triggers.
        /// Returns metrics about the purge operation.
        /// </summary>
        public async Task<AuditRetentionMetrics> PurgeAuditLogs(CancellationToken cancellationToken = default)
        {
            var retentionMonths = _configuration.GetValue<int>("InventoryAuditRetention:Months", 9);
            var isDryRun = _configuration.GetValue<bool>("InventoryAuditRetention:DryRun", false);

            var startTime = DateTime.UtcNow;
            var cutoffDate = DateTime.UtcNow.AddMonths(-retentionMonths);

            var metrics = new AuditRetentionMetrics
            {
                RetentionMonths = retentionMonths,
                CutoffDate = cutoffDate,
                IsDryRun = isDryRun,
                StartTime = startTime
            };

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<CloudWatcherContext>();

                    // Count records that would be deleted
                    var recordsToDelete = await dbContext.InventoryAuditLogs
                        .Where(x => x.ChangedAt < cutoffDate)
                        .CountAsync(cancellationToken);

                    metrics.RecordsConsidered = recordsToDelete;

                    if (isDryRun)
                    {
                        _logger.LogInformation(
                            "InventoryAuditRetentionService DRY RUN: Would delete {RecordsConsidered} records older than {CutoffDate}",
                            recordsToDelete, cutoffDate);
                    }
                    else
                    {
                        // Batch delete in chunks to avoid locking issues
                        var batchSize = 1000;
                        var recordsDeleted = 0;

                        while (true)
                        {
                            var batch = await dbContext.InventoryAuditLogs
                                .Where(x => x.ChangedAt < cutoffDate)
                                .Take(batchSize)
                                .ToListAsync(cancellationToken);

                            if (!batch.Any())
                                break;

                            dbContext.InventoryAuditLogs.RemoveRange(batch);
                            await dbContext.SaveChangesAsync(cancellationToken);
                            recordsDeleted += batch.Count;

                            _logger.LogInformation(
                                "InventoryAuditRetentionService: Deleted {DeletedCount} records in batch",
                                batch.Count);
                        }

                        metrics.RecordsDeleted = recordsDeleted;

                        _logger.LogInformation(
                            "InventoryAuditRetentionService: Purge completed. Deleted {RecordsDeleted} of {RecordsConsidered} records older than {CutoffDate}",
                            recordsDeleted, recordsToDelete, cutoffDate);
                    }
                }

                metrics.EndTime = DateTime.UtcNow;
                metrics.Success = true;
            }
            catch (Exception ex)
            {
                metrics.EndTime = DateTime.UtcNow;
                metrics.Success = false;
                metrics.ErrorMessage = ex.Message;

                _logger.LogError(ex,
                    "InventoryAuditRetentionService: Error during purge operation");
            }

            return metrics;
        }
    }

    /// <summary>
    /// Metrics returned from audit retention purge operation.
    /// </summary>
    public class AuditRetentionMetrics
    {
        public int RetentionMonths { get; set; }
        public DateTime CutoffDate { get; set; }
        public bool IsDryRun { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int RecordsConsidered { get; set; }
        public int RecordsDeleted { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
    }
}
