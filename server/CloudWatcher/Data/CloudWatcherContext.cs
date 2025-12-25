using Microsoft.EntityFrameworkCore;
using CloudWatcher.Models;

namespace CloudWatcher.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for CloudWatcher platform
    /// Manages all database entities and relationships for the system
    /// </summary>
    public class CloudWatcherContext : DbContext
    {
        public CloudWatcherContext(DbContextOptions<CloudWatcherContext> options)
            : base(options)
        {
        }

        // Identity & RBAC
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<UserDepartment> UserDepartments { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;

        // Request/Response Tracking
        public DbSet<Request> Requests { get; set; } = null!;
        public DbSet<Response> Responses { get; set; } = null!;
        public DbSet<RequestMetadata> RequestMetadata { get; set; } = null!;
        public DbSet<ResponseMetadata> ResponseMetadata { get; set; } = null!;
        public DbSet<DeviceConnection> DeviceConnections { get; set; } = null!;
        public DbSet<CloudFileReference> CloudFileReferences { get; set; } = null!;

        // Parts & Inventory
        public DbSet<Part> Parts { get; set; } = null!;
        public DbSet<PartVariant> PartVariants { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<PartSupplier> PartSuppliers { get; set; } = null!;
        public DbSet<Inventory> Inventory { get; set; } = null!;
        public DbSet<StockLevel> StockLevels { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<PartConsumable> PartConsumables { get; set; } = null!;

        // Orders & Workflows
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<OrderApproval> OrderApprovals { get; set; } = null!;
        public DbSet<OrderHistory> OrderHistory { get; set; } = null!;

        // Audit & Compliance
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<AgentDecision> AgentDecisions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Identity & RBAC
            ConfigureIdentityEntities(modelBuilder);

            // Configure Request/Response tracking
            ConfigureRequestResponseEntities(modelBuilder);

            // Configure Parts & Inventory
            ConfigurePartsInventoryEntities(modelBuilder);

            // Configure Orders & Workflows
            ConfigureOrderEntities(modelBuilder);

            // Configure Audit & Compliance
            ConfigureAuditEntities(modelBuilder);
        }

        private void ConfigureIdentityEntities(ModelBuilder modelBuilder)
        {
            // Users: Primary key, unique email, OAuth fields
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.OAuthId)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.IsActive);

            // Roles
            modelBuilder.Entity<Role>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // Departments with self-referencing hierarchy
            modelBuilder.Entity<Department>()
                .HasKey(d => d.Id);
            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Name);
            modelBuilder.Entity<Department>()
                .HasOne<Department>()
                .WithMany()
                .HasForeignKey(d => d.ParentId)
                .IsRequired(false);
            modelBuilder.Entity<Department>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.ManagerId)
                .IsRequired(false);

            // UserRole junction table
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<UserRole>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(ur => ur.UserId);
            modelBuilder.Entity<UserRole>()
                .HasOne<Role>()
                .WithMany()
                .HasForeignKey(ur => ur.RoleId);

            // UserDepartment junction table
            modelBuilder.Entity<UserDepartment>()
                .HasKey(ud => new { ud.UserId, ud.DepartmentId });
            modelBuilder.Entity<UserDepartment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(ud => ud.UserId);
            modelBuilder.Entity<UserDepartment>()
                .HasOne<Department>()
                .WithMany()
                .HasForeignKey(ud => ud.DepartmentId);

            // RolePermission junction table
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });
            modelBuilder.Entity<RolePermission>()
                .HasOne<Role>()
                .WithMany()
                .HasForeignKey(rp => rp.RoleId);
        }

        private void ConfigureRequestResponseEntities(ModelBuilder modelBuilder)
        {
            // Requests
            modelBuilder.Entity<Request>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Request>()
                .HasIndex(r => r.DeviceId);
            modelBuilder.Entity<Request>()
                .HasIndex(r => r.Status);
            modelBuilder.Entity<Request>()
                .HasIndex(r => r.CreatedAt);

            // Responses
            modelBuilder.Entity<Response>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Response>()
                .HasOne<Request>()
                .WithMany()
                .HasForeignKey(r => r.RequestId);
            modelBuilder.Entity<Response>()
                .HasIndex(r => r.Status);

            // RequestMetadata - key-value pairs for dynamic data
            modelBuilder.Entity<RequestMetadata>()
                .HasKey(rm => rm.Id);
            modelBuilder.Entity<RequestMetadata>()
                .HasOne<Request>()
                .WithMany()
                .HasForeignKey(rm => rm.RequestId);

            // ResponseMetadata - key-value pairs
            modelBuilder.Entity<ResponseMetadata>()
                .HasKey(rm => rm.Id);
            modelBuilder.Entity<ResponseMetadata>()
                .HasOne<Response>()
                .WithMany()
                .HasForeignKey(rm => rm.ResponseId);

            // DeviceConnections
            modelBuilder.Entity<DeviceConnection>()
                .HasKey(dc => dc.Id);
            modelBuilder.Entity<DeviceConnection>()
                .HasIndex(dc => dc.DeviceId);
            modelBuilder.Entity<DeviceConnection>()
                .HasIndex(dc => dc.ConnectedAt);

            // CloudFileReferences
            modelBuilder.Entity<CloudFileReference>()
                .HasKey(cf => cf.Id);
            modelBuilder.Entity<CloudFileReference>()
                .HasOne<Request>()
                .WithMany()
                .HasForeignKey(cf => cf.RequestId);
            modelBuilder.Entity<CloudFileReference>()
                .HasIndex(cf => cf.Provider);
        }

        private void ConfigurePartsInventoryEntities(ModelBuilder modelBuilder)
        {
            // Parts
            modelBuilder.Entity<Part>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<Part>()
                .HasIndex(p => p.Code)
                .IsUnique();

            // PartVariants
            modelBuilder.Entity<PartVariant>()
                .HasKey(pv => pv.Id);
            modelBuilder.Entity<PartVariant>()
                .HasOne<Part>()
                .WithMany()
                .HasForeignKey(pv => pv.PartId);
            modelBuilder.Entity<PartVariant>()
                .HasIndex(pv => pv.VariantCode);

            // Suppliers
            modelBuilder.Entity<Supplier>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.Name);

            // PartSuppliers junction table
            modelBuilder.Entity<PartSupplier>()
                .HasKey(ps => new { ps.PartId, ps.SupplierId });
            modelBuilder.Entity<PartSupplier>()
                .HasOne<Part>()
                .WithMany()
                .HasForeignKey(ps => ps.PartId);
            modelBuilder.Entity<PartSupplier>()
                .HasOne<Supplier>()
                .WithMany()
                .HasForeignKey(ps => ps.SupplierId);

            // Locations
            modelBuilder.Entity<Location>()
                .HasKey(l => l.Id);
            modelBuilder.Entity<Location>()
                .HasOne<Department>()
                .WithMany()
                .HasForeignKey(l => l.DepartmentId);

            // Inventory
            modelBuilder.Entity<Inventory>()
                .HasKey(i => i.Id);
            modelBuilder.Entity<Inventory>()
                .HasOne<Part>()
                .WithMany()
                .HasForeignKey(i => i.PartId);
            modelBuilder.Entity<Inventory>()
                .HasOne<Location>()
                .WithMany()
                .HasForeignKey(i => i.LocationId);

            // StockLevels
            modelBuilder.Entity<StockLevel>()
                .HasKey(sl => new { sl.PartId, sl.LocationId });
            modelBuilder.Entity<StockLevel>()
                .HasOne<Part>()
                .WithMany()
                .HasForeignKey(sl => sl.PartId);
            modelBuilder.Entity<StockLevel>()
                .HasOne<Location>()
                .WithMany()
                .HasForeignKey(sl => sl.LocationId);
            modelBuilder.Entity<StockLevel>()
                .HasIndex(sl => sl.LastUpdated);

            // PartConsumables
            modelBuilder.Entity<PartConsumable>()
                .HasKey(pc => new { pc.PartId, pc.LocationId });
            modelBuilder.Entity<PartConsumable>()
                .HasOne<Part>()
                .WithMany()
                .HasForeignKey(pc => pc.PartId);
            modelBuilder.Entity<PartConsumable>()
                .HasOne<Location>()
                .WithMany()
                .HasForeignKey(pc => pc.LocationId);
        }

        private void ConfigureOrderEntities(ModelBuilder modelBuilder)
        {
            // Orders
            modelBuilder.Entity<Order>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<Order>()
                .HasOne<Request>()
                .WithMany()
                .HasForeignKey(o => o.RequestId);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            // OrderItems
            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => oi.Id);
            modelBuilder.Entity<OrderItem>()
                .HasOne<Order>()
                .WithMany()
                .HasForeignKey(oi => oi.OrderId);
            modelBuilder.Entity<OrderItem>()
                .HasOne<Part>()
                .WithMany()
                .HasForeignKey(oi => oi.PartId);

            // OrderApprovals
            modelBuilder.Entity<OrderApproval>()
                .HasKey(oa => oa.Id);
            modelBuilder.Entity<OrderApproval>()
                .HasOne<Order>()
                .WithMany()
                .HasForeignKey(oa => oa.OrderId);
            modelBuilder.Entity<OrderApproval>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(oa => oa.ApproverId);

            // OrderHistory
            modelBuilder.Entity<OrderHistory>()
                .HasKey(oh => oh.Id);
            modelBuilder.Entity<OrderHistory>()
                .HasOne<Order>()
                .WithMany()
                .HasForeignKey(oh => oh.OrderId);
            modelBuilder.Entity<OrderHistory>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(oh => oh.UserId);
        }

        private void ConfigureAuditEntities(ModelBuilder modelBuilder)
        {
            // AuditLogs
            modelBuilder.Entity<AuditLog>()
                .HasKey(al => al.Id);
            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.TableName);
            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Operation);
            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Timestamp);
            modelBuilder.Entity<AuditLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(al => al.UserId);

            // AgentDecisions
            modelBuilder.Entity<AgentDecision>()
                .HasKey(ad => ad.Id);
            modelBuilder.Entity<AgentDecision>()
                .HasOne<Request>()
                .WithMany()
                .HasForeignKey(ad => ad.RequestId);
            modelBuilder.Entity<AgentDecision>()
                .HasIndex(ad => ad.AgentName);
            modelBuilder.Entity<AgentDecision>()
                .HasIndex(ad => ad.Timestamp);
        }
    }
}
