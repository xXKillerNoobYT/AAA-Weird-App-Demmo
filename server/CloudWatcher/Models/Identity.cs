using System;
using System.Collections.Generic;

namespace CloudWatcher.Models
{
    /// <summary>
    /// User entity - represents system users with OAuth support
    /// </summary>
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? OAuthProvider { get; set; } // 'azure-ad', 'google', 'local'
        public string? OAuthId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Role entity - represents system roles with permissions
    /// </summary>
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Sample roles: admin, manager, technician, viewer
    }

    /// <summary>
    /// Department entity - represents organizational departments with hierarchy
    /// </summary>
    public class Department
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public Guid? ParentId { get; set; } // Self-referencing for hierarchy
        public string? Description { get; set; }
        public Guid? ManagerId { get; set; } // FK to User
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// UserRole junction table - maps users to roles
    /// </summary>
    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }

    /// <summary>
    /// UserDepartment junction table - maps users to departments
    /// </summary>
    public class UserDepartment
    {
        public Guid UserId { get; set; }
        public Guid DepartmentId { get; set; }
    }

    /// <summary>
    /// RolePermission junction table - maps roles to permissions
    /// </summary>
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public string PermissionId { get; set; } = null!;
        // Example permissions: 'read', 'write', 'delete', 'admin'
    }
}
