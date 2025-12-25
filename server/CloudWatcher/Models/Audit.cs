using System;

namespace CloudWatcher.Models
{
    /// <summary>
    /// AuditLog entity - comprehensive audit trail for compliance
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TableName { get; set; } = null!;
        public string Operation { get; set; } = null!; // INSERT, UPDATE, DELETE
        public Guid? UserId { get; set; }
        public string? OldValues { get; set; } // JSON of previous state
        public string? NewValues { get; set; } // JSON of new state
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AgentDecision entity - tracks AI agent decisions for explainability
    /// </summary>
    public class AgentDecision
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? RequestId { get; set; }
        public string AgentName { get; set; } = null!;
        public string Decision { get; set; } = null!; // The decision made by the agent
        public string? Context { get; set; } // JSON context used for decision
        public decimal? Confidence { get; set; } // 0.0 - 1.0 confidence score
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
