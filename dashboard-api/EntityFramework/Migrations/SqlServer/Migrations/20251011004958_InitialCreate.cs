using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "global"),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "0.0.0.0"),
                    Scope = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    EvaluationModes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    ScheduledEnableDate = table.Column<DateTimeOffset>(type: "DATETIMEOFFSET", nullable: true),
                    ScheduledDisableDate = table.Column<DateTimeOffset>(type: "DATETIMEOFFSET", nullable: true),
                    WindowStartTime = table.Column<TimeOnly>(type: "TIME", nullable: true),
                    WindowEndTime = table.Column<TimeOnly>(type: "TIME", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WindowDays = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    TargetingRules = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    EnabledUsers = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    DisabledUsers = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    UserPercentageEnabled = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    EnabledTenants = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    DisabledTenants = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "[]"),
                    TenantPercentageEnabled = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    Variations = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "{}"),
                    DefaultVariation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "off")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => new { x.Key, x.ApplicationName, x.ApplicationVersion });
                    table.CheckConstraint("CK_DisabledTenants_json", "ISJSON(DisabledTenants) = 1");
                    table.CheckConstraint("CK_DisabledUsers_json", "ISJSON(DisabledUsers) = 1");
                    table.CheckConstraint("CK_EnabledTenants_json", "ISJSON(EnabledTenants) = 1");
                    table.CheckConstraint("CK_EnabledUsers_json", "ISJSON(EnabledUsers) = 1");
                    table.CheckConstraint("CK_EvaluationModes_json", "ISJSON(EvaluationModes) = 1");
                    table.CheckConstraint("CK_TargetingRules_json", "ISJSON(TargetingRules) = 1");
                    table.CheckConstraint("CK_TenantPercentage", "TenantPercentageEnabled >= 0 AND TenantPercentageEnabled <= 100");
                    table.CheckConstraint("CK_UserPercentage", "UserPercentageEnabled >= 0 AND UserPercentageEnabled <= 100");
                    table.CheckConstraint("CK_Variations_json", "ISJSON(Variations) = 1");
                    table.CheckConstraint("CK_WindowDays_json", "ISJSON(WindowDays) = 1");
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagsAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FlagKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, defaultValue: "global"),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "0.0.0.0"),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "DATETIMEOFFSET", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Notes = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagsAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagsMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FlagKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "global"),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "0.0.0.0"),
                    IsPermanent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExpirationDate = table.Column<DateTimeOffset>(type: "DATETIMEOFFSET", nullable: false),
                    Tags = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagsMetadata", x => x.Id);
                    table.CheckConstraint("CK_metadata_tags_json", "ISJSON(Tags) = 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagsAudit_FlagKey_ApplicationName_ApplicationVersion",
                table: "FeatureFlagsAudit",
                columns: new[] { "FlagKey", "ApplicationName", "ApplicationVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagsMetadata_FlagKey_ApplicationName_ApplicationVersion",
                table: "FeatureFlagsMetadata",
                columns: new[] { "FlagKey", "ApplicationName", "ApplicationVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "FeatureFlagsAudit");

            migrationBuilder.DropTable(
                name: "FeatureFlagsMetadata");
        }
    }
}
