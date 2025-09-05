using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirebirdApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    token = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    user_email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    stored_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "database_configs",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    server = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    database_path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    port = table.Column<int>(type: "INTEGER", nullable: false),
                    charset = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    file_size_bytes = table.Column<long>(type: "INTEGER", nullable: true),
                    last_size_check = table.Column<DateTime>(type: "TEXT", nullable: true),
                    desktop_node_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    is_default = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_database_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "database_snapshots",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    database_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    database_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    generated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    snapshot_data = table.Column<string>(type: "TEXT", nullable: false),
                    file_path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    total_tables = table.Column<int>(type: "INTEGER", nullable: false),
                    processed_tables = table.Column<int>(type: "INTEGER", nullable: false),
                    current_table = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    progress_percentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    estimated_completion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_database_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "local_nodes",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    machine_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    operating_system = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    system_version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    architecture = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    port = table.Column<int>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_seen = table.Column<DateTime>(type: "TEXT", nullable: false),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    is_anonymous = table.Column<bool>(type: "INTEGER", nullable: false),
                    anonymous_token = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    anonymous_expires_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    default_database_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snapshot_cache",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    database_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    cache_key = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    cached_data = table.Column<string>(type: "TEXT", nullable: false),
                    cached_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snapshot_table_columns",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    snapshot_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    table_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    column_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    data_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    length = table.Column<int>(type: "INTEGER", nullable: true),
                    precision = table.Column<int>(type: "INTEGER", nullable: true),
                    scale = table.Column<int>(type: "INTEGER", nullable: true),
                    is_nullable = table.Column<bool>(type: "INTEGER", nullable: false),
                    default_value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    is_primary_key = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_table_columns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snapshot_tables",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    snapshot_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    table_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    schema_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    table_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    record_count = table.Column<long>(type: "INTEGER", nullable: false),
                    last_id = table.Column<long>(type: "INTEGER", nullable: true),
                    generated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_tables", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_tokens_token",
                table: "auth_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_tokens_user_id",
                table: "auth_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_database_configs_is_active",
                table: "database_configs",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_database_configs_is_default",
                table: "database_configs",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_database_snapshots_database_id",
                table: "database_snapshots",
                column: "database_id");

            migrationBuilder.CreateIndex(
                name: "IX_database_snapshots_generated_at",
                table: "database_snapshots",
                column: "generated_at");

            migrationBuilder.CreateIndex(
                name: "IX_database_snapshots_status",
                table: "database_snapshots",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_local_nodes_is_active",
                table: "local_nodes",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_local_nodes_is_anonymous",
                table: "local_nodes",
                column: "is_anonymous");

            migrationBuilder.CreateIndex(
                name: "IX_local_nodes_machine_name",
                table: "local_nodes",
                column: "machine_name");

            migrationBuilder.CreateIndex(
                name: "IX_local_nodes_user_id",
                table: "local_nodes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_cache_database_id_cache_key",
                table: "snapshot_cache",
                columns: new[] { "database_id", "cache_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_cache_expires_at",
                table: "snapshot_cache",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_cache_is_active",
                table: "snapshot_cache",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_table_columns_snapshot_id",
                table: "snapshot_table_columns",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_table_columns_snapshot_id_table_name",
                table: "snapshot_table_columns",
                columns: new[] { "snapshot_id", "table_name" });

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_tables_snapshot_id",
                table: "snapshot_tables",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_tables_snapshot_id_table_name",
                table: "snapshot_tables",
                columns: new[] { "snapshot_id", "table_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_tokens");

            migrationBuilder.DropTable(
                name: "database_configs");

            migrationBuilder.DropTable(
                name: "database_snapshots");

            migrationBuilder.DropTable(
                name: "local_nodes");

            migrationBuilder.DropTable(
                name: "project_settings");

            migrationBuilder.DropTable(
                name: "snapshot_cache");

            migrationBuilder.DropTable(
                name: "snapshot_table_columns");

            migrationBuilder.DropTable(
                name: "snapshot_tables");
        }
    }
}
