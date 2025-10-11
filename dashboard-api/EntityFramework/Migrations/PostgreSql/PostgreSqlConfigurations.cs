using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.PostgreSql;

public static class PostgreSqlConfigurations
{
	public class FeatureFlagAuditConfiguration : IEntityTypeConfiguration<FeatureFlagAudit>
	{
		public void Configure(EntityTypeBuilder<FeatureFlagAudit> builder)
		{
			// Table mapping
			builder.ToTable("feature_flags_audit");

			// Primary key
			builder.HasKey(e => e.Id);

			// Ignore the navigation property back to FeatureFlag
			builder.Ignore(e => e.FeatureFlag);

			// Column mappings
			builder.Property(e => e.Id)
				.HasColumnName("id")
				.HasDefaultValueSql("gen_random_uuid()"); // PostgreSQL function

			builder.Property(e => e.FlagKey)
				.HasColumnName("flag_key")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.ApplicationName)
				.HasColumnName("application_name")
				.HasMaxLength(255)
				.HasDefaultValue("global");

			builder.Property(e => e.ApplicationVersion)
				.HasColumnName("application_version")
				.HasMaxLength(100)
				.HasDefaultValue("0.0.0.0")
				.IsRequired();

			builder.Property(e => e.Action)
				.HasColumnName("action")
				.HasMaxLength(50)
				.IsRequired();

			builder.Property(e => e.Actor)
				.HasColumnName("actor")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.Timestamp)
				.HasColumnName("timestamp")
				.HasDefaultValueSql("NOW()") // PostgreSQL function
				.IsRequired();

			builder.Property(e => e.Notes)
				.HasColumnName("notes");

			// Create index on the foreign key columns (without FK constraint)
			builder.HasIndex(e => new { e.FlagKey, e.ApplicationName, e.ApplicationVersion });
		}
	}

	public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
	{
		public void Configure(EntityTypeBuilder<FeatureFlag> builder)
		{
			// Table mapping with check constraints
			builder.ToTable("feature_flags", t =>
			{
				t.HasCheckConstraint("CK_FeatureFlag_UserPercentage",
					"user_percentage_enabled >= 0 AND user_percentage_enabled <= 100");
				t.HasCheckConstraint("CK_FeatureFlag_TenantPercentage",
					"tenant_percentage_enabled >= 0 AND tenant_percentage_enabled <= 100");
			});

			// Composite primary key
			builder.HasKey(e => new { e.Key, e.ApplicationName, e.ApplicationVersion });

			// Ignore navigation properties - they will not be included in migrations
			builder.Ignore(e => e.Metadata);
			builder.Ignore(e => e.AuditTrail);

			// Column mappings and constraints
			builder.Property(e => e.Key)
				.HasColumnName("key")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.ApplicationName)
				.HasColumnName("application_name")
				.HasMaxLength(255)
				.HasDefaultValue("global")
				.IsRequired();

			builder.Property(e => e.ApplicationVersion)
				.HasColumnName("application_version")
				.HasMaxLength(100)
				.HasDefaultValue("0.0.0.0")
				.IsRequired();

			builder.Property(e => e.Scope)
				.HasColumnName("scope")
				.HasDefaultValue(0)
				.IsRequired();

			builder.Property(e => e.Name)
				.HasColumnName("name")
				.HasMaxLength(500)
				.IsRequired();

			builder.Property(e => e.Description)
				.HasColumnName("description")
				.HasDefaultValue("")
				.IsRequired();

			// JSON columns - will be configured differently per provider
			builder.Property(e => e.EvaluationModes)
				.HasColumnName("evaluation_modes")
				.HasColumnType("jsonb")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.WindowDays)
				.HasColumnName("window_days")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.TargetingRules)
				.HasColumnName("targeting_rules")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.EnabledUsers)
				.HasColumnName("enabled_users")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.DisabledUsers)
				.HasColumnName("disabled_users")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.EnabledTenants)
				.HasColumnName("enabled_tenants")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.DisabledTenants)
				.HasColumnName("disabled_tenants")
				.HasDefaultValue("[]")
				.IsRequired();

			builder.Property(e => e.Variations)
				.HasColumnName("variations")
				.HasDefaultValue("{}")
				.IsRequired();

			// Scheduling columns
			builder.Property(e => e.ScheduledEnableDate)
				.HasColumnName("scheduled_enable_date");

			builder.Property(e => e.ScheduledDisableDate)
				.HasColumnName("scheduled_disable_date");

			// Time window columns
			builder.Property(e => e.WindowStartTime)
				.HasColumnName("window_start_time");

			builder.Property(e => e.WindowEndTime)
				.HasColumnName("window_end_time");

			builder.Property(e => e.TimeZone)
				.HasColumnName("time_zone")
				.HasMaxLength(100);

			// Percentage columns with check constraints
			builder.Property(e => e.UserPercentageEnabled)
				.HasColumnName("user_percentage_enabled")
				.HasDefaultValue(100)
				.IsRequired();

			builder.Property(e => e.TenantPercentageEnabled)
				.HasColumnName("tenant_percentage_enabled")
				.HasDefaultValue(100)
				.IsRequired();

			builder.Property(e => e.DefaultVariation)
				.HasColumnName("default_variation")
				.HasMaxLength(255)
				.HasDefaultValue("off")
				.IsRequired();

			// PostgreSQL - use JSONB
			builder.Property(e => e.EvaluationModes).HasColumnType("jsonb");
			builder.Property(e => e.WindowDays).HasColumnType("jsonb");
			builder.Property(e => e.TargetingRules).HasColumnType("jsonb");
			builder.Property(e => e.EnabledUsers).HasColumnType("jsonb");
			builder.Property(e => e.DisabledUsers).HasColumnType("jsonb");
			builder.Property(e => e.EnabledTenants).HasColumnType("jsonb");
			builder.Property(e => e.DisabledTenants).HasColumnType("jsonb");
			builder.Property(e => e.Variations).HasColumnType("jsonb");
		}
	}

	public class FeatureFlagMetadataConfiguration : IEntityTypeConfiguration<FeatureFlagMetadata>
	{
		public void Configure(EntityTypeBuilder<FeatureFlagMetadata> builder)
		{
			// Table mapping
			builder.ToTable("feature_flags_metadata");

			// Primary key
			builder.HasKey(e => e.Id);

			// Ignore the navigation property back to FeatureFlag
			builder.Ignore(e => e.FeatureFlag);

			// Column mappings
			builder.Property(e => e.Id)
				.HasColumnName("id")
				.HasDefaultValueSql("gen_random_uuid()");

			builder.Property(e => e.FlagKey)
				.HasColumnName("flag_key")
				.HasMaxLength(255)
				.IsRequired();

			builder.Property(e => e.ApplicationName)
				.HasColumnName("application_name")
				.HasMaxLength(255)
				.HasDefaultValue("global")
				.IsRequired();

			builder.Property(e => e.ApplicationVersion)
				.HasColumnName("application_version")
				.HasMaxLength(100)
				.HasDefaultValue("0.0.0.0")
				.IsRequired();

			builder.Property(e => e.IsPermanent)
				.HasColumnName("is_permanent")
				.HasDefaultValue(false)
				.IsRequired();

			builder.Property(e => e.ExpirationDate)
				.HasColumnName("expiration_date")
				.IsRequired();

			builder.Property(e => e.Tags)
				.HasColumnName("tags")
				.HasDefaultValue("{}")
				.IsRequired();

			builder.Property(e => e.Tags).HasColumnType("jsonb");

			// Create index on the foreign key columns (without FK constraint)
			builder.HasIndex(e => new { e.FlagKey, e.ApplicationName, e.ApplicationVersion })
				.IsUnique();
		}
	}
}
