using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloudWatcher.Controllers
{
    /// <summary>
    /// Base controller for all CloudWatcher API endpoints.
    /// Provides common functionality: error handling, logging, validation, authentication.
    /// </summary>
    [ApiController]
    [Route("api/v2/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Gets the logger instance for the derived controller.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the current authenticated user ID from JWT token.
        /// </summary>
        protected Guid? CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the current user's email from JWT token.
        /// </summary>
        protected string? CurrentUserEmail
        {
            get => User.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Gets the current user's roles from JWT token.
        /// </summary>
        protected IEnumerable<string> CurrentUserRoles
        {
            get => User.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets whether the current user is authenticated.
        /// </summary>
        protected bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// Initialize base controller with logger.
        /// </summary>
        protected BaseApiController(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a standardized success response.
        /// </summary>
        protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Success", int statusCode = 200)
        {
            var response = new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// Creates a standardized error response.
        /// </summary>
        protected ActionResult<ApiResponse> ErrorResponse(string message, string? errorCode = null, int statusCode = 400)
        {
            var response = new ApiResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                Timestamp = DateTime.UtcNow
            };

            Logger.LogError("API Error ({ErrorCode}): {Message}", errorCode ?? "UNKNOWN", message);
            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// Creates a standardized error response with detailed information.
        /// </summary>
        protected ActionResult<ApiErrorResponse> ErrorResponseWithDetails(
            string message, 
            Dictionary<string, object> details,
            string? errorCode = null, 
            int statusCode = 400)
        {
            var response = new ApiErrorResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            Logger.LogError("API Error ({ErrorCode}): {Message}. Details: {@Details}", 
                errorCode ?? "UNKNOWN", message, details);
            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// Validates that the user has the required permission.
        /// Throws UnauthorizedAccessException if not authorized.
        /// </summary>
        protected void RequirePermission(string requiredPermission)
        {
            if (!IsAuthenticated)
            {
                Logger.LogWarning("Unauthenticated access attempt to {Action}", ControllerContext.RouteData.Values["action"]);
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            var userClaims = User.FindAll("permissions")?.Select(c => c.Value) ?? new List<string>();
            if (!userClaims.Contains(requiredPermission))
            {
                Logger.LogWarning("Unauthorized access: User {UserId} lacks permission {Permission}", 
                    CurrentUserId, requiredPermission);
                throw new UnauthorizedAccessException($"User lacks required permission: {requiredPermission}");
            }
        }

        /// <summary>
        /// Validates that the user has one of the required roles.
        /// Throws UnauthorizedAccessException if not authorized.
        /// </summary>
        protected void RequireRole(params string[] requiredRoles)
        {
            if (!IsAuthenticated)
            {
                Logger.LogWarning("Unauthenticated access attempt to {Action}", ControllerContext.RouteData.Values["action"]);
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            var userRoles = CurrentUserRoles.ToList();
            if (!userRoles.Any(r => requiredRoles.Contains(r)))
            {
                Logger.LogWarning("Unauthorized access: User {UserId} lacks required roles {Roles}", 
                    CurrentUserId, string.Join(", ", requiredRoles));
                throw new UnauthorizedAccessException($"User lacks required role(s): {string.Join(", ", requiredRoles)}");
            }
        }

        /// <summary>
        /// Validates model state and returns error response if invalid.
        /// </summary>
        protected bool ValidateModelState<T>(out ActionResult? errorResult) where T : class
        {
            if (!ModelState.IsValid)
            {
                var errors = new Dictionary<string, object>();
                foreach (var state in ModelState)
                {
                    if (state.Value?.Errors.Count > 0)
                    {
                        errors[state.Key] = state.Value.Errors.Select(e => e.ErrorMessage).ToList();
                    }
                }

                errorResult = ErrorResponseWithDetails("Model validation failed", errors, "VALIDATION_ERROR", 400).Result!;
                Logger.LogWarning("Model validation failed with errors: {@Errors}", errors);
                return false;
            }

            errorResult = null;
            return true;
        }

        /// <summary>
        /// Handles exceptions and returns appropriate error response.
        /// </summary>
        protected ActionResult HandleException(Exception ex, string defaultMessage = "An unexpected error occurred")
        {
            Logger.LogError(ex, "Unhandled exception in {Controller}.{Action}", 
                ControllerContext.RouteData.Values["controller"], 
                ControllerContext.RouteData.Values["action"]);

            if (ex is UnauthorizedAccessException)
            {
                return ErrorResponse(ex.Message, "UNAUTHORIZED", 401).Result!;
            }

            if (ex is ArgumentException argEx)
            {
                return ErrorResponse(argEx.Message, "INVALID_ARGUMENT", 400).Result!;
            }

            if (ex is InvalidOperationException opEx)
            {
                return ErrorResponse(opEx.Message, "INVALID_OPERATION", 409).Result!;
            }

            if (ex is KeyNotFoundException notFoundEx)
            {
                return ErrorResponse(notFoundEx.Message, "NOT_FOUND", 404).Result!;
            }

            return ErrorResponse(defaultMessage, "INTERNAL_ERROR", 500).Result!;
        }
    }
}
