using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.SqlServer;

public static class SqlServerConfigurations
{
	public class FeatureFlagAuditConfiguration : IEntityTypeConfiguration<FeatureFlagAudit>
	{
		public void Configure(EntityTypeBuilder<FeatureFlagAudit> builder)
		{
			// Table mapping
			builder.ToTable("FeatureFlagsAudit");

			// Primary key
			builder.HasKey(e => e.Id);

			// Ignore the navigation property back to FeatureFlag
			builder.Ignore(e => e.FeatureFlag);

			// Column mappings
			builder.Property(e => e.Id)
				.HasColumnName("Id")
				.HasDefaultValueSql("NEWID()"); // SQL Server function

			builder.Property(e => e.FlagKey)
				.HasColumnName("FlagKey")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.ApplicationName)
				.HasColumnName("ApplicationName")
				.HasMaxLength(255)
				.HasDefaultValue("global");

			builder.Property(e => e.ApplicationVersion)
				.HasColumnName("ApplicationVersion")
				.HasMaxLength(100)
				.HasDefaultValue("0.0.0.0")
				.IsRequired();

			builder.Property(e => e.Action)
				.HasColumnName("Action")
				.HasMaxLength(50)
				.IsRequired();

			builder.Property(e => e.Actor)
				.HasColumnName("Actor")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.Timestamp)
				.HasColumnName("Timestamp")
				.HasColumnType("DATETIMEOFFSET")
				.HasDefaultValueSql("GETUTCDATE()") // SQL Server function
				.IsRequired();

			builder.Property(e => e.Notes)
				.HasColumnName("Notes")
				.HasColumnType("NVARCHAR(MAX)");

			// Create index on the foreign key columns (without FK constraint)
			builder.HasIndex(e => new { e.FlagKey, e.ApplicationName, e.ApplicationVersion });
		}
	}

	public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
	{
		public void Configure(EntityTypeBuilder<FeatureFlag> builder)
		{
			// Table mapping with check constraints
			builder.ToTable("FeatureFlags", t =>
			{
				t.HasCheckConstraint("CK_UserPercentage",
					"UserPercentageEnabled >= 0 AND UserPercentageEnabled <= 100");
				t.HasCheckConstraint("CK_TenantPercentage",
					"TenantPercentageEnabled >= 0 AND TenantPercentageEnabled <= 100");
				t.HasCheckConstraint("CK_EvaluationModes_json", "ISJSON(EvaluationModes) = 1");
				t.HasCheckConstraint("CK_WindowDays_json", "ISJSON(WindowDays) = 1");
				t.HasCheckConstraint("CK_TargetingRules_json", "ISJSON(TargetingRules) = 1");
				t.HasCheckConstraint("CK_EnabledUsers_json", "ISJSON(EnabledUsers) = 1");
				t.HasCheckConstraint("CK_DisabledUsers_json", "ISJSON(DisabledUsers) = 1");
				t.HasCheckConstraint("CK_EnabledTenants_json", "ISJSON(EnabledTenants) = 1");
				t.HasCheckConstraint("CK_DisabledTenants_json", "ISJSON(DisabledTenants) = 1");
				t.HasCheckConstraint("CK_Variations_json", "ISJSON(Variations) = 1");
			});

			// Composite primary key
			builder.HasKey(e => new { e.Key, e.ApplicationName, e.ApplicationVersion });

			// Ignore navigation properties - they will not be included in migrations
			builder.Ignore(e => e.Metadata);
			builder.Ignore(e => e.AuditTrail);

			// Column mappings and constraints
			builder.Property(e => e.Key)
				.HasColumnName("Key")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.ApplicationName)
				.HasColumnName("ApplicationName")
				.HasMaxLength(255)
				.HasDefaultValue("global")
				.IsRequired();

			builder.Property(e => e.ApplicationVersion)
				.HasColumnName("ApplicationVersion")
				.HasMaxLength(100)
				.HasDefaultValue("0.0.0.0")
				.IsRequired();

			builder.Property(e => e.Scope)
				.HasColumnName("Scope")
				.HasDefaultValue(0)
				.IsRequired();

			builder.Property(e => e.Name)
				.HasColumnName("Name")
				.HasMaxLength(500)
				.IsRequired();

			builder.Property(e => e.Description)
				.HasColumnName("Description")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("")
				.IsRequired();

			// JSON columns - SQL Server uses NVARCHAR(MAX)
			builder.Property(e => e.EvaluationModes)
				.HasColumnName("EvaluationModes")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.WindowDays)
				.HasColumnName("WindowDays")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.TargetingRules)
				.HasColumnName("TargetingRules")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.EnabledUsers)
				.HasColumnName("EnabledUsers")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.DisabledUsers)
				.HasColumnName("DisabledUsers")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.EnabledTenants)
				.HasColumnName("EnabledTenants")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.DisabledTenants)
				.HasColumnName("DisabledTenants")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.Variations)
				.HasColumnName("Variations")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("{}")
				.IsRequired();

			// Scheduling columns
			builder.Property(e => e.ScheduledEnableDate)
				.HasColumnName("ScheduledEnableDate")
				.HasColumnType("DATETIMEOFFSET");

			builder.Property(e => e.ScheduledDisableDate)
				.HasColumnName("ScheduledDisableDate")
				.HasColumnType("DATETIMEOFFSET");

			// Time window columns
			builder.Property(e => e.WindowStartTime)
				.HasColumnName("WindowStartTime")
				.HasColumnType("TIME");

			builder.Property(e => e.WindowEndTime)
				.HasColumnName("WindowEndTime")
				.HasColumnType("TIME");

			builder.Property(e => e.TimeZone)
				.HasColumnName("TimeZone")
				.HasMaxLength(100);

			// Percentage columns
			builder.Property(e => e.UserPercentageEnabled)
				.HasColumnName("UserPercentageEnabled")
				.HasDefaultValue(100)
				.IsRequired();

			builder.Property(e => e.TenantPercentageEnabled)
				.HasColumnName("TenantPercentageEnabled")
				.HasDefaultValue(100)
				.IsRequired();

			builder.Property(e => e.DefaultVariation)
				.HasColumnName("DefaultVariation")
				.HasMaxLength(255)
				.HasDefaultValue("")
				.IsRequired();
		}
	}

	public class FeatureFlagMetadataConfiguration : IEntityTypeConfiguration<FeatureFlagMetadata>
	{
		public void Configure(EntityTypeBuilder<FeatureFlagMetadata> builder)
		{
			// Table mapping
			builder.ToTable("FeatureFlagsMetadata", t =>
			{
				t.HasCheckConstraint("CK_metadata_tags_json", "ISJSON(Tags) = 1");
			});

			// Primary key
			builder.HasKey(e => e.Id);

			// Ignore the navigation property back to FeatureFlag
			builder.Ignore(e => e.FeatureFlag);

			// Column mappings
			builder.Property(e => e.Id)
				.HasColumnName("Id")
				.HasDefaultValueSql("NEWID()"); // SQL Server function

			builder.Property(e => e.FlagKey)
				.HasColumnName("FlagKey")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.ApplicationName)
				.HasColumnName("ApplicationName")
				.HasMaxLength(255)
				.HasDefaultValue("global")
				.IsRequired();

			builder.Property(e => e.ApplicationVersion)
				.HasColumnName("ApplicationVersion")
				.HasMaxLength(100)
				.HasDefaultValue("0.0.0.0")
				.IsRequired();

			builder.Property(e => e.IsPermanent)
				.HasColumnName("IsPermanent")
				.HasDefaultValue(false)
				.IsRequired();

			builder.Property(e => e.ExpirationDate)
				.HasColumnName("ExpirationDate")
				.HasColumnType("DATETIMEOFFSET")
				.IsRequired();

			builder.Property(e => e.Tags)
				.HasColumnName("Tags")
				.HasColumnType("NVARCHAR(MAX)")
				.HasDefaultValue("{}")
				.IsRequired();

			// Create index on the foreign key columns (without FK constraint)
			builder.HasIndex(e => new { e.FlagKey, e.ApplicationName, e.ApplicationVersion })
				.IsUnique();
		}
	}

	public class UserConfiguration : IEntityTypeConfiguration<User>
	{
		public void Configure(EntityTypeBuilder<User> builder)
		{
			builder.ToTable("FeatureFlagsUsers");
			builder.HasKey(u => u.Username);

			builder.Property(e => e.Username)
				.HasColumnName("UserName")  // Fixed: Username maps to UserName column
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.Password)  // Fixed: PasswordHash property (not Username again!)
				.HasColumnName("Password")
				.IsRequired();

			builder.Property(e => e.Role)
				.HasColumnName("Role")
				.HasMaxLength(255)
				.IsRequired()
				.HasDefaultValue("Viewer");

			builder.Property(e => e.CreatedAt)
				.HasColumnName("CreatedAt")
				.HasColumnType("DATETIMEOFFSET")
				.HasDefaultValueSql("GETUTCDATE()")
				.IsRequired();

			builder.Property(e => e.LastLoginAt)
				.HasColumnName("LastLoginAt")
				.HasColumnType("DATETIMEOFFSET")
				.IsRequired(false);

			builder.Property(e => e.IsActive)
				.HasColumnName("IsActive")
				.HasDefaultValue(true)  // Changed to true - users should be active by default
				.IsRequired();

			builder.Property(e => e.ForcePasswordChange)
				.HasColumnName("ForcePasswordChange")
				.HasDefaultValue(false)  
				.IsRequired();
		}
	}
}
