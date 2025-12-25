namespace CloudWatcher.Controllers
{
    /// <summary>
    /// Standard API response wrapper for successful operations.
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message about the operation result.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error code (only populated for failed responses).
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// UTC timestamp when the response was generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Generic API response wrapper for successful operations with typed data.
    /// </summary>
    /// <typeparam name="T">The type of data returned in the response.</typeparam>
    public class ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// The data returned by the API operation.
        /// </summary>
        public T? Data { get; set; }
    }

    /// <summary>
    /// API error response with detailed error information.
    /// </summary>
    public class ApiErrorResponse : ApiResponse
    {
        /// <summary>
        /// Detailed error information (field-level validation errors, etc.).
        /// </summary>
        public Dictionary<string, object>? Details { get; set; }
    }

    /// <summary>
    /// Paginated response wrapper for list endpoints.
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated list.</typeparam>
    public class PaginatedResponse<T> : ApiResponse<List<T>>
    {
        /// <summary>
        /// Pagination metadata.
        /// </summary>
        public PaginationInfo? Pagination { get; set; }
    }

    /// <summary>
    /// Pagination metadata for list responses.
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// Current page number (0-based).
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items available.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total number of pages available.
        /// </summary>
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;

        /// <summary>
        /// Whether there are more pages after the current one.
        /// </summary>
        public bool HasMore => Page < TotalPages - 1;
    }
}
