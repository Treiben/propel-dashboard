using Microsoft.EntityFrameworkCore;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Utilities;
using System.Text.Json;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;

public class SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : DashboardDbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.FeatureFlagConfiguration());
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.FeatureFlagMetadataConfiguration());
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.FeatureFlagAuditConfiguration());
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.UserConfiguration());
	}
}

public class SqlServerProvider(SqlServerDbContext context) : IDatabaseProvider
{
	public DashboardDbContext Context => context;

	public async Task<FeatureFlag> CreateAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
	{
		var identifier = flag.Identifier;
		var metadata = flag.Administration;
		var config = flag.EvaluationOptions;
		var lastModified = metadata.ChangeHistory[^1];

		await context.Database.ExecuteSqlRawAsync(@"
        INSERT INTO FeatureFlags (
            [Key], ApplicationName, ApplicationVersion, Scope, [Name], [Description],
            EvaluationModes, ScheduledEnableDate, ScheduledDisableDate,
            WindowStartTime, WindowEndTime, TimeZone, WindowDays,
            TargetingRules, EnabledUsers, DisabledUsers, UserPercentageEnabled,
            EnabledTenants, DisabledTenants, TenantPercentageEnabled,
            Variations, DefaultVariation
        ) VALUES (
            {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12},
            {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}
        );

        INSERT INTO FeatureFlagsMetadata (
            FlagKey, ApplicationName, ApplicationVersion,
            IsPermanent, ExpirationDate, Tags
        ) VALUES ({0}, {1}, {2}, {22}, {23}, {24});

        INSERT INTO FeatureFlagsAudit (
            FlagKey, ApplicationName, ApplicationVersion,
            [Action], Actor, Notes, [Timestamp]
        ) VALUES ({0}, {1}, {2}, {25}, {26}, {27}, {28});",
		identifier.Key,
		identifier.ApplicationName ?? "global",
		identifier.ApplicationVersion ?? "0.0.0.0",
		(int)identifier.Scope,
		metadata.Name,
		metadata.Description,
		JsonSerializer.Serialize(config.ModeSet.Modes.Select(m => (int)m).ToArray()),
		config.Schedule.HasSchedule() ? (DateTimeOffset)config.Schedule.EnableOn : null!,
		config.Schedule.HasSchedule() ? (DateTimeOffset)config.Schedule.DisableOn : null!,
		config.OperationalWindow.HasWindow() ? config.OperationalWindow.StartOn : null!,
		config.OperationalWindow.HasWindow() ? config.OperationalWindow.StopOn : null!,
		config.OperationalWindow.HasWindow() ? config.OperationalWindow.TimeZone : null!,
		JsonSerializer.Serialize(config.OperationalWindow.DaysActive.Select(d => (int)d).ToArray()),
		JsonSerializer.Serialize(config.TargetingRules, JsonDefaults.JsonOptions),
		JsonSerializer.Serialize(config.UserAccessControl.Allowed),
		JsonSerializer.Serialize(config.UserAccessControl.Blocked),
		config.UserAccessControl.RolloutPercentage,
		JsonSerializer.Serialize(config.TenantAccessControl.Allowed),
		JsonSerializer.Serialize(config.TenantAccessControl.Blocked),
		config.TenantAccessControl.RolloutPercentage,
		JsonSerializer.Serialize(config.Variations.Values),
		config.Variations.DefaultVariation,
		metadata.RetentionPolicy.IsPermanent,
		(DateTimeOffset)metadata.RetentionPolicy.ExpirationDate,
		JsonSerializer.Serialize(metadata.Tags),
		lastModified.Action,
		lastModified.Actor,
		lastModified.Notes,
		(DateTimeOffset)lastModified.Timestamp);

		return flag;
	}

	public async Task<FeatureFlag> UpdateAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
	{
		var identifier = flag.Identifier;
		var metadata = flag.Administration;
		var config = flag.EvaluationOptions;
		var lastModified = metadata.ChangeHistory[^1];

		var updatedRows = await context.Database.ExecuteSqlRawAsync(@"
        UPDATE FeatureFlags SET
            [Name] = {4}, [Description] = {5}, EvaluationModes = {6},
            ScheduledEnableDate = {7}, ScheduledDisableDate = {8},
            WindowStartTime = {9}, WindowEndTime = {10}, TimeZone = {11},
            WindowDays = {12}, TargetingRules = {13},
            EnabledUsers = {14}, DisabledUsers = {15}, UserPercentageEnabled = {16},
            EnabledTenants = {17}, DisabledTenants = {18}, TenantPercentageEnabled = {19},
            Variations = {20}, DefaultVariation = {21}
        WHERE [Key] = {0} AND ApplicationName = {1} AND ApplicationVersion = {2} AND Scope = {3};

        INSERT INTO FeatureFlagsAudit (
            FlagKey, ApplicationName, ApplicationVersion,
            [Action], Actor, Notes, [Timestamp]
        ) VALUES ({0}, {1}, {2}, {22}, {23}, {24}, {25});",
		identifier.Key,
		identifier.ApplicationName ?? "global",
		identifier.ApplicationVersion ?? "0.0.0.0",
		(int)identifier.Scope,
		metadata.Name,
		metadata.Description,
		JsonSerializer.Serialize(config.ModeSet.Modes.Select(m => (int)m).ToArray()),
		config.Schedule.HasSchedule() ? (DateTimeOffset)config.Schedule.EnableOn : null!,
		config.Schedule.HasSchedule() ? (DateTimeOffset)config.Schedule.DisableOn : null!,
		config.OperationalWindow.HasWindow() ? config.OperationalWindow.StartOn : null!,
		config.OperationalWindow.HasWindow() ? config.OperationalWindow.StopOn : null!,
		config.OperationalWindow.HasWindow() ? config.OperationalWindow.TimeZone : null!,
		JsonSerializer.Serialize(config.OperationalWindow.DaysActive.Select(d => (int)d).ToArray()),
		JsonSerializer.Serialize(config.TargetingRules, JsonDefaults.JsonOptions),
		JsonSerializer.Serialize(config.UserAccessControl.Allowed),
		JsonSerializer.Serialize(config.UserAccessControl.Blocked),
		config.UserAccessControl.RolloutPercentage,
		JsonSerializer.Serialize(config.TenantAccessControl.Allowed),
		JsonSerializer.Serialize(config.TenantAccessControl.Blocked),
		config.TenantAccessControl.RolloutPercentage,
		JsonSerializer.Serialize(config.Variations.Values),
		config.Variations.DefaultVariation,
		lastModified.Action,
		lastModified.Actor,
		lastModified.Notes,
		(DateTimeOffset)lastModified.Timestamp);

		if (updatedRows == 0)
		{
			throw new InvalidOperationException($"Feature flag with key '{identifier.Key}' not found");
		}

		return flag;
	}

	public async Task<FeatureFlag> UpdateMetadataAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
	{
		var identifier = flag.Identifier;
		var metadata = flag.Administration;
		var lastModified = metadata.ChangeHistory[^1];

		await context.Database.ExecuteSqlRawAsync(@"
		UPDATE FeatureFlags
		SET Name = {4}, Description = {5}
		WHERE [Key] = {0} AND ApplicationName = {1} AND ApplicationVersion = {2} AND Scope = {3};

        UPDATE FeatureFlagsMetadata 
        SET IsPermanent = {6}, 
			ExpirationDate = {7}, 
			Tags = {8}
		 WHERE FlagKey = {0} AND ApplicationName = {1} AND ApplicationVersion = {2};

        INSERT INTO FeatureFlagsAudit (
            FlagKey, ApplicationName, ApplicationVersion,
            Action, Actor, Notes, Timestamp
        ) VALUES ({0}, {1}, {2}, {9}, {10}, {11}, {12});",
			identifier.Key,
			identifier.ApplicationName ?? "global",
			identifier.ApplicationVersion ?? "0.0.0.0",
			identifier.Scope,
			metadata.Name,
			metadata.Description,
			metadata.RetentionPolicy.IsPermanent,
			(DateTimeOffset)metadata.RetentionPolicy.ExpirationDate,
			JsonSerializer.Serialize(metadata.Tags),
			lastModified.Action,
			lastModified.Actor,
			lastModified.Notes,
			(DateTimeOffset)lastModified.Timestamp);

		return flag;
	}

	public async Task<bool> DeleteAsync(FlagIdentifier identifier, string userid, string notes, CancellationToken cancellationToken = default)
	{
		var deletedRows = await context.Database.ExecuteSqlRawAsync(@"
        INSERT INTO FeatureFlagsAudit (
            FlagKey, ApplicationName, ApplicationVersion,
            [Action], Actor, Notes, [Timestamp]
        ) VALUES ({0}, {1}, {2}, 'flag-deleted', {3}, {4}, {5});

        DELETE FROM FeatureFlagsMetadata 
        WHERE FlagKey = {0} AND ApplicationName = {1} AND ApplicationVersion = {2};

        DELETE FROM FeatureFlags 
        WHERE [Key] = {0} AND ApplicationName = {1} AND ApplicationVersion = {2} AND Scope = {6};",
		identifier.Key,
		identifier.ApplicationName ?? "global",
		identifier.ApplicationVersion ?? "0.0.0.0",
		userid,
		notes,
		DateTimeOffset.UtcNow,
		(int)identifier.Scope);

		return deletedRows > 0;
	}

	public string BuildFilterQuery(int page, int pageSize, FeatureFlagFilter? filter)
	{
		var sql = $@"
			SELECT 
				ff.*, 
				ffm.FlagKey as FlagKey, 
				ffm.IsPermanent as IsPermanent, 
				ffm.ExpirationDate as ExpirationDate, 
				ffm.Tags as Tags, 
				ffa.[Action] as [Action], 
				ffa.Actor as Actor, 
				ffa.[Timestamp] as [Timestamp], 
				ffa.Notes as Notes 
			FROM 
				FeatureFlags ff 
			LEFT JOIN 
				FeatureFlagsMetadata ffm ON ff.[Key] = ffm.FlagKey 
					AND ff.ApplicationName = ffm.ApplicationName 
					AND ff.ApplicationVersion = ffm.ApplicationVersion 
			LEFT JOIN 
				FeatureFlagsAudit ffa ON ff.[Key] = ffa.FlagKey 
					AND ff.ApplicationName = ffa.ApplicationName 
					AND ff.ApplicationVersion = ffa.ApplicationVersion 
					AND ffa.[Timestamp] = (
						SELECT MAX(ffa2.[Timestamp]) 
						FROM FeatureFlagsAudit ffa2 
						WHERE ffa2.FlagKey = ff.[Key] 
							AND ffa2.ApplicationName = ff.ApplicationName 
							AND ffa2.ApplicationVersion = ff.ApplicationVersion
					)";

		if (filter != null)
		{
			var (whereClause, _) = BuildFilterConditions(filter);
			sql += $" {whereClause} ";
		}

		return sql += $@"
			ORDER BY ff.[Name], ff.[Key] 
			OFFSET {(page - 1) * pageSize} ROWS 
			FETCH NEXT {pageSize} ROWS ONLY";
	}

	public string BuildCountQuery(FeatureFlagFilter? filter)
	{
		var sql = $@"SELECT COUNT(*) 
		FROM FeatureFlags ff 
			LEFT JOIN FeatureFlagsMetadata ffm ON ff.[Key] = ffm.FlagKey 
					AND ff.ApplicationName = ffm.ApplicationName 
					AND ff.ApplicationVersion = ffm.ApplicationVersion";

		if (filter != null)
		{
			var (whereClause, _) = BuildFilterConditions(filter);
			return sql += $@"{whereClause}";
		}

		return sql;
	}

	public (string whereClause, Dictionary<string, object> parameters) BuildFilterConditions(FeatureFlagFilter? filter)
	{
		var conditions = new List<string>();
		var parameters = new Dictionary<string, object>();

		if (filter == null)
			return (string.Empty, parameters);

		// Application name filtering - use ff table (primary identifier)
		if (!string.IsNullOrEmpty(filter.ApplicationName))
		{
			var appNameParam = "appName";
			parameters[appNameParam] = filter.ApplicationName;
			conditions.Add($"ff.ApplicationName = @{appNameParam}");
		}

		// Flag scope filtering
		if (filter.Scope.HasValue)
		{
			var scopeParam = "scope";
			parameters[scopeParam] = (int)filter.Scope.Value;
			conditions.Add($"ff.Scope = @{scopeParam}");
		}

		// Flag permanent retention filtering
		if (filter.PermanentFlagsOnly.HasValue)
		{
			var permanentParam = "IsPermanent";
			parameters[permanentParam] = filter.PermanentFlagsOnly.Value;
			conditions.Add($"ffm.IsPermanent = {{{permanentParam}}}");
		}

		// Evaluation modes filtering - use ff table (main flag data)
		if (filter.EvaluationModes != null && filter.EvaluationModes.Length > 0)
		{
			var modeConditions = new List<string>();
			for (int i = 0; i < filter.EvaluationModes.Length; i++)
			{
				var modeParam = $"mode{i}";
				var modeJson = JsonSerializer.Serialize(new[] { (int)filter.EvaluationModes[i] }, JsonDefaults.JsonOptions);
				parameters[modeParam] = modeJson;
				modeConditions.Add($@"EXISTS (
					SELECT 1 FROM OPENJSON(ff.EvaluationModes) 
					WHERE value IN (SELECT value FROM OPENJSON(@{modeParam}))
				)");
			}
			conditions.Add($"({string.Join(" OR ", modeConditions)})");
		}

		// Tags filtering - use ffm table (metadata specific)
		if (filter.Tags != null && filter.Tags.Count > 0)
		{
			var tagConditions = new List<string>();
			var tagIndex = 0;

			foreach (var tag in filter.Tags)
			{
				var keyParam = $"tagKey{tagIndex}";
				if (string.IsNullOrEmpty(tag.Value))
				{
					// Search by tag key only
					parameters[keyParam] = tag.Key;
					tagConditions.Add($@"EXISTS (
						SELECT 1 FROM OPENJSON(ffm.Tags) 
						WHERE [key] = @{keyParam}
					)");
				}
				else
				{
					// Search by exact key-value match
					var tagParam = $"tag{tagIndex}";
					var tagJson = JsonSerializer.Serialize(new Dictionary<string, string> { [tag.Key] = tag.Value }, JsonDefaults.JsonOptions);
					parameters[tagParam] = tagJson;
					tagConditions.Add($@"EXISTS (
						SELECT 1 
						FROM OPENJSON(ffm.Tags) 
						WHERE [key] = @{keyParam} AND [value] = @{tagParam}
					)");
					parameters[keyParam] = tag.Key;
					parameters[tagParam] = tag.Value;
				}
				tagIndex++;
			}

			if (tagConditions.Count > 0)
			{
				conditions.Add($"({string.Join(" OR ", tagConditions)})");
			}
		}

		var whereClause = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : string.Empty;
		return (whereClause, parameters);
	}
}

public static class RegisterSqlServerProviderExtensions
{
	public static IServiceCollection AddSqlServerProvider(this IServiceCollection services, string connectionString)
	{
		services.AddDbContext<SqlServerDbContext>(options =>
		{
			options.UseSqlServer(connectionString, sqlOptions =>
			{
				sqlOptions.EnableRetryOnFailure(
					maxRetryCount: 3,
					maxRetryDelay: TimeSpan.FromSeconds(5),
					errorNumbersToAdd: null);
				sqlOptions.MigrationsAssembly(typeof(SqlServerDbContext).Assembly.FullName);
			});

			// Configure for development/production
			options.EnableSensitiveDataLogging(false);
			options.EnableDetailedErrors(false);
		});

		services.AddScoped<IDatabaseProvider, SqlServerProvider>();
		return services;
	}
}