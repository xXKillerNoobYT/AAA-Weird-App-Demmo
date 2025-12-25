using System;
using System.Collections.Generic;

namespace CloudWatcher.Models
{
    /// <summary>
    /// Request entity - represents device requests sent to CloudWatcher
    /// </summary>
    public class Request
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DeviceId { get; set; } = null!;
        public string Type { get; set; } = null!; // 'get_parts', 'order_parts', 'status_check'
        public string Status { get; set; } = "pending"; // pending, processing, completed, failed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Response entity - represents CloudWatcher responses to device requests
    /// </summary>
    public class Response
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RequestId { get; set; }
        public string Status { get; set; } = "pending"; // pending, sent, acknowledged, failed
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// RequestMetadata - dynamic key-value pairs for request data
    /// </summary>
    public class RequestMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RequestId { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    /// <summary>
    /// ResponseMetadata - dynamic key-value pairs for response data
    /// </summary>
    public class ResponseMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResponseId { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    /// <summary>
    /// DeviceConnection - tracks device connectivity status
    /// </summary>
    public class DeviceConnection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DeviceId { get; set; } = null!;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DisconnectedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// CloudFileReference - tracks files stored in cloud for requests/responses
    /// </summary>
    public class CloudFileReference
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RequestId { get; set; }
        public string CloudPath { get; set; } = null!; // S3, Azure Blob, GCP path
        public string Provider { get; set; } = null!; // 's3', 'azure', 'gcp'
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
