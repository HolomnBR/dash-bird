using Microsoft.EntityFrameworkCore;
using FirebirdApi.Models;

namespace FirebirdApi.Data
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
        {
        }

        public DbSet<AuthToken> AuthTokens { get; set; }
        public DbSet<DatabaseConfigSqlite> DatabaseConfigs { get; set; }
        public DbSet<DatabaseSnapshotSqlite> DatabaseSnapshots { get; set; }
        public DbSet<SnapshotTable> SnapshotTables { get; set; }
        public DbSet<SnapshotTableColumn> SnapshotTableColumns { get; set; }
        public DbSet<ProjectSettingsSqlite> ProjectSettingsSqlite { get; set; }
        public DbSet<SnapshotCache> SnapshotCache { get; set; }
        public DbSet<LocalNode> LocalNodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurações para AuthToken
            modelBuilder.Entity<AuthToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(4000);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserEmail).HasMaxLength(255);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
            });

            // Configurações para DatabaseConfigSqlite
            modelBuilder.Entity<DatabaseConfigSqlite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Server).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Database).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Charset).HasMaxLength(50);
                entity.Property(e => e.DesktopNodeId).HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(100);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDefault);
            });

            // Configurações para DatabaseSnapshotSqlite
            modelBuilder.Entity<DatabaseSnapshotSqlite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);
                entity.Property(e => e.DatabaseId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.SnapshotData).IsRequired();
                entity.Property(e => e.FilePath).HasMaxLength(500);
                entity.Property(e => e.CurrentTable).HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                entity.Property(e => e.ProgressPercentage).HasPrecision(5, 2);
                entity.HasIndex(e => e.DatabaseId);
                entity.HasIndex(e => e.GeneratedAt);
                entity.HasIndex(e => e.Status);
            });

            // Configurações para SnapshotTable
            modelBuilder.Entity<SnapshotTable>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SnapshotId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TableName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Schema).HasMaxLength(100);
                entity.Property(e => e.TableType).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.HasIndex(e => e.SnapshotId);
                entity.HasIndex(e => new { e.SnapshotId, e.TableName });
            });

            // Configurações para SnapshotTableColumn
            modelBuilder.Entity<SnapshotTableColumn>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SnapshotId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TableName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ColumnName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.DataType).HasMaxLength(100);
                entity.Property(e => e.DefaultValue).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.HasIndex(e => e.SnapshotId);
                entity.HasIndex(e => new { e.SnapshotId, e.TableName });
            });

            // Configurações para ProjectSettingsSqlite
            modelBuilder.Entity<ProjectSettingsSqlite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DefaultDatabaseId).HasMaxLength(100);
            });


            // Configurações para SnapshotCache
            modelBuilder.Entity<SnapshotCache>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DatabaseId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CacheKey).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CachedData).IsRequired();
                entity.HasIndex(e => new { e.DatabaseId, e.CacheKey }).IsUnique();
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsActive);
            });

            // Configurações para LocalNode
            modelBuilder.Entity<LocalNode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);
                entity.Property(e => e.MachineName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.OperatingSystem).IsRequired().HasMaxLength(255);
                entity.Property(e => e.SystemVersion).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Architecture).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 suporta até 45 caracteres
                entity.Property(e => e.UserId).HasMaxLength(100);
                entity.Property(e => e.AnonymousToken).HasMaxLength(4000);
                entity.HasIndex(e => e.MachineName);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsAnonymous);
                entity.HasIndex(e => e.UserId);
            });
        }
    }
}
