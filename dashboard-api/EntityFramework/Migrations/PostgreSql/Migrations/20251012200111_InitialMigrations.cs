using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    application_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "global"),
                    application_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "0.0.0.0"),
                    scope = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    evaluation_modes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    scheduled_enable_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_disable_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    window_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    window_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    window_days = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    targeting_rules = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    enabled_users = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    disabled_users = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    user_percentage_enabled = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    enabled_tenants = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    disabled_tenants = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    tenant_percentage_enabled = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    variations = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    default_variation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "off")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags", x => new { x.key, x.application_name, x.application_version });
                    table.CheckConstraint("CK_FeatureFlag_TenantPercentage", "tenant_percentage_enabled >= 0 AND tenant_percentage_enabled <= 100");
                    table.CheckConstraint("CK_FeatureFlag_UserPercentage", "user_percentage_enabled >= 0 AND user_percentage_enabled <= 100");
                });

            migrationBuilder.CreateTable(
                name: "feature_flags_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    flag_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    application_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, defaultValue: "global"),
                    application_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "0.0.0.0"),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags_audit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    flag_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    application_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "global"),
                    application_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "0.0.0.0"),
                    is_permanent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    expiration_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tags = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature-flags-users",
                columns: table => new
                {
                    username = table.Column<string>(name: "user-name", type: "character varying(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "Viewer"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature-flags-users", x => x.username);
                });

            migrationBuilder.CreateIndex(
                name: "IX_feature_flags_audit_flag_key_application_name_application_v~",
                table: "feature_flags_audit",
                columns: new[] { "flag_key", "application_name", "application_version" });

            migrationBuilder.CreateIndex(
                name: "IX_feature_flags_metadata_flag_key_application_name_applicatio~",
                table: "feature_flags_metadata",
                columns: new[] { "flag_key", "application_name", "application_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "feature_flags_audit");

            migrationBuilder.DropTable(
                name: "feature_flags_metadata");

            migrationBuilder.DropTable(
                name: "feature-flags-users");
        }
    }
}
