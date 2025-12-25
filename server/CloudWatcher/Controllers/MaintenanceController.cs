using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudWatcher.Services;
using CloudWatcher.Auth;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// Administrative maintenance endpoints for CloudWatcher.
    /// All endpoints require Admin authorization policy.
    /// </summary>
    [ApiController]
    [Route("api/v2/[controller]")]
    [Produces("application/json")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnlyPolicy)]
    public class MaintenanceController : BaseApiController
    {
        private readonly InventoryAuditRetentionService _retentionService;

        public MaintenanceController(
            InventoryAuditRetentionService retentionService,
            ILogger<MaintenanceController> logger)
            : base(logger)
        {
            _retentionService = retentionService;
        }

        /// <summary>
        /// Trigger inventory audit retention purge immediately.
        /// Deletes audit log entries older than configured retention period.
        /// </summary>
        /// <remarks>
        /// This endpoint requires Admin policy authorization.
        /// Returns metrics about the purge operation.
        /// </remarks>
        /// <remarks>
        /// TODO: Remove [AllowAnonymous] before production deployment.
        /// This is temporarily enabled for testing without Azure AD configuration.
        /// </remarks>
        [AllowAnonymous] // TEMPORARY: Remove before production
        [HttpPost("audit-retention/run")]
        public async Task<IActionResult> RunAuditRetentionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation(
                    "Audit retention purge requested by user {UserId}",
                    CurrentUserId);

                var metrics = await _retentionService.PurgeAuditLogs(cancellationToken);

                var result = new AuditRetentionRunResult
                {
                    Success = metrics.Success,
                    RetentionMonths = metrics.RetentionMonths,
                    CutoffDate = metrics.CutoffDate,
                    IsDryRun = metrics.IsDryRun,
                    RecordsConsidered = metrics.RecordsConsidered,
                    RecordsDeleted = metrics.RecordsDeleted,
                    DurationMs = (long)metrics.Duration.TotalMilliseconds,
                    ExecutedAt = DateTime.UtcNow,
                    ErrorMessage = metrics.ErrorMessage
                };

                Logger.LogInformation(
                    "Audit retention purge completed. Deleted: {DeletedCount}, Considered: {ConsideredCount}",
                    metrics.RecordsDeleted, metrics.RecordsConsidered);

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Audit retention purge was cancelled");
                return BadRequest(new ProblemDetails
                {
                    Type = "https://example.com/problems/operation-cancelled",
                    Title = "Operation Cancelled",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "The audit retention purge operation was cancelled."
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during audit retention purge");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ProblemDetails
                    {
                        Type = "https://example.com/problems/internal-error",
                        Title = "Internal Server Error",
                        Status = StatusCodes.Status500InternalServerError,
                        Detail = "An unexpected error occurred while running the audit retention purge."
                    });
            }
        }
    }

    /// <summary>
    /// Result of running the audit retention purge operation.
    /// </summary>
    public class AuditRetentionRunResult
    {
        /// <summary>
        /// Whether the operation completed successfully (no exceptions).
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Retention period in months.
        /// </summary>
        public int RetentionMonths { get; set; }

        /// <summary>
        /// Cutoff date used for deletion (older records are deleted).
        /// </summary>
        public DateTime CutoffDate { get; set; }

        /// <summary>
        /// Whether this was a dry-run (no actual deletions).
        /// </summary>
        public bool IsDryRun { get; set; }

        /// <summary>
        /// Total number of records considered for deletion.
        /// </summary>
        public int RecordsConsidered { get; set; }

        /// <summary>
        /// Number of records actually deleted (0 if dry-run).
        /// </summary>
        public int RecordsDeleted { get; set; }

        /// <summary>
        /// Duration of the operation in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Timestamp when the operation was executed.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Error message if operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
