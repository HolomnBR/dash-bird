using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirebirdApi.Models
{
    /// <summary>
    /// Tabela para armazenar tokens de autenticação
    /// </summary>
    [Table("auth_tokens")]
    public class AuthToken
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("token")]
        public string Token { get; set; } = string.Empty;

        [Required]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("user_email")]
        public string? UserEmail { get; set; }

        [Column("stored_at")]
        public DateTime StoredAt { get; set; } = DateTime.UtcNow;

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Tabela para armazenar configurações de banco de dados
    /// </summary>
    [Table("database_configs")]
    public class DatabaseConfigSqlite
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("server")]
        public string Server { get; set; } = string.Empty;

        [Required]
        [Column("database_path")]
        public string Database { get; set; } = string.Empty;

        [Required]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("port")]
        public int Port { get; set; } = 3050;

        [Column("charset")]
        public string Charset { get; set; } = "UTF8";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("file_size_bytes")]
        public long? FileSizeBytes { get; set; }

        [Column("last_size_check")]
        public DateTime? LastSizeCheck { get; set; }

        [Column("desktop_node_id")]
        public string? DesktopNodeId { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// Tabela para armazenar snapshots de banco de dados
    /// </summary>
    [Table("database_snapshots")]
    public class DatabaseSnapshotSqlite
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("database_id")]
        public string DatabaseId { get; set; } = string.Empty;

        [Required]
        [Column("database_name")]
        public string DatabaseName { get; set; } = string.Empty;

        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [Column("snapshot_data")]
        public string SnapshotData { get; set; } = string.Empty; // JSON serializado

        [Column("file_path")]
        public string? FilePath { get; set; } // Caminho do arquivo JSON se ainda existir

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Campos de progresso
        [Column("total_tables")]
        public int TotalTables { get; set; } = 0;

        [Column("processed_tables")]
        public int ProcessedTables { get; set; } = 0;

        [Column("current_table")]
        public string? CurrentTable { get; set; }

        [Column("progress_percentage")]
        public decimal ProgressPercentage { get; set; } = 0;

        [Column("status")]
        public string Status { get; set; } = "Starting"; // Starting, InProgress, Completed, Error

        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("estimated_completion")]
        public DateTime? EstimatedCompletion { get; set; }
    }

    /// <summary>
    /// Tabela para armazenar informações de tabelas dos snapshots
    /// </summary>
    [Table("snapshot_tables")]
    public class SnapshotTable
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("snapshot_id")]
        public string SnapshotId { get; set; } = string.Empty;

        [Required]
        [Column("table_name")]
        public string TableName { get; set; } = string.Empty;

        [Column("schema_name")]
        public string? Schema { get; set; }

        [Column("table_type")]
        public string? TableType { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("record_count")]
        public long RecordCount { get; set; } = 0;

        [Column("last_id")]
        public long? LastId { get; set; }

        [Column("generated_at")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Tabela para armazenar colunas das tabelas dos snapshots
    /// </summary>
    [Table("snapshot_table_columns")]
    public class SnapshotTableColumn
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("snapshot_id")]
        public string SnapshotId { get; set; } = string.Empty;

        [Required]
        [Column("table_name")]
        public string TableName { get; set; } = string.Empty;

        [Required]
        [Column("column_name")]
        public string ColumnName { get; set; } = string.Empty;

        [Column("data_type")]
        public string? DataType { get; set; }

        [Column("length")]
        public int? Length { get; set; }

        [Column("precision")]
        public int? Precision { get; set; }

        [Column("scale")]
        public int? Scale { get; set; }

        [Column("is_nullable")]
        public bool IsNullable { get; set; } = true;

        [Column("default_value")]
        public string? DefaultValue { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_primary_key")]
        public bool IsPrimaryKey { get; set; } = false;
    }

    /// <summary>
    /// Tabela para configurações do projeto
    /// </summary>
    [Table("project_settings")]
    public class ProjectSettingsSqlite
    {
        [Key]
        [Column("id")]
        public int Id { get; set; } = 1; // Sempre 1, pois só temos uma configuração

        [Column("default_database_id")]
        public string? DefaultDatabaseId { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }


    /// <summary>
    /// Tabela para cache de snapshots otimizado
    /// </summary>
    [Table("snapshot_cache")]
    public class SnapshotCache
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("database_id")]
        public string DatabaseId { get; set; } = string.Empty;

        [Required]
        [Column("cache_key")]
        public string CacheKey { get; set; } = string.Empty;

        [Column("cached_data")]
        public string CachedData { get; set; } = string.Empty; // JSON serializado

        [Column("cached_at")]
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Tabela para armazenar informações do nó local
    /// </summary>
    [Table("local_nodes")]
    public class LocalNode
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("machine_id")]
        public string MachineId { get; set; } = string.Empty;

        [Required]
        [Column("machine_name")]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        [Column("operating_system")]
        public string OperatingSystem { get; set; } = string.Empty;

        [Required]
        [Column("system_version")]
        public string SystemVersion { get; set; } = string.Empty;

        [Required]
        [Column("architecture")]
        public string Architecture { get; set; } = string.Empty;

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("port")]
        public int Port { get; set; } = 8000;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_seen")]
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("is_anonymous")]
        public bool IsAnonymous { get; set; } = true;

        [Column("anonymous_token")]
        public string? AnonymousToken { get; set; }

        [Column("anonymous_expires_at")]
        public DateTime? AnonymousExpiresAt { get; set; }
    }
}
