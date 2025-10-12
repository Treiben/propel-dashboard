using Knara.UtcStrict;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Domain;
using System.Text.Json;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework;

public static class Mapper
{
	public static FeatureFlag MapToDomain(Entities.FeatureFlag entity)
	{
		// Create identifier
		var identifier = new FlagIdentifier(
			entity.Key,
			(Scope)entity.Scope,
			entity.ApplicationName,
			entity.ApplicationVersion);

		// Create metadata
		var metadata = MapMetadataToDomain(entity);

		// Create configuration
		var configuration = MapConfigurationToDomain(entity);

		return new FeatureFlag(identifier, metadata, configuration);
	}

	public static FlagAdministration MapMetadataToDomain(Entities.FeatureFlag entity) => new(
			Name: entity.Name,
			Description: entity.Description ?? string.Empty,
			RetentionPolicy: new RetentionPolicy(
				entity.Metadata.IsPermanent,
				entity.Metadata.ExpirationDate,
				new FlagLockPolicy([.. Parser.ParseEvaluationModes(entity.EvaluationModes).Modes])),
			Tags: JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Metadata.Tags) ?? [],
			ChangeHistory: [.. entity.AuditTrail
				.OrderByDescending(t => t.Timestamp)
				.Select(MapAuditTrailToDomain)]);

	public static AuditTrail MapAuditTrailToDomain(Entities.FeatureFlagAudit entity) => new(
				Timestamp: entity.Timestamp,
				Actor: entity.Actor,
				Action: entity.Action,
				Notes: entity.Notes!
			);

	public static FlagEvaluationOptions MapConfigurationToDomain(Entities.FeatureFlag entity)
	{
		// Parse evaluation modes
		ModeSet evaluationModes = Parser.ParseEvaluationModes(entity.EvaluationModes);

		// Parse schedule
		var schedule =new  UtcSchedule(entity.ScheduledEnableDate ?? UtcDateTime.MinValue,
													entity.ScheduledDisableDate ?? UtcDateTime.MaxValue);

		// Parse operational window
		var alwaysopen = UtcTimeWindow.AlwaysOpen;
		var operationalWindow = new UtcTimeWindow(
				entity.WindowStartTime?.ToTimeSpan() ?? alwaysopen.StartOn,
				entity.WindowEndTime?.ToTimeSpan() ?? alwaysopen.StopOn,
				entity.TimeZone ?? "UTC",
				Parser.ParseWindowDays(entity.WindowDays));

		// Parse targeting rules
		var targetingRules = Parser.ParseTargetingRules(entity.TargetingRules);

		// Parse user access control
		var userAccessControl = new AccessControl(
			Parser.ParseStringList(entity.EnabledUsers),
			Parser.ParseStringList(entity.DisabledUsers),
			entity.UserPercentageEnabled);

		// Parse tenant access control
		var tenantAccessControl = new AccessControl(
			Parser.ParseStringList(entity.EnabledTenants),
			Parser.ParseStringList(entity.DisabledTenants),
			entity.TenantPercentageEnabled);

		// Parse variations
		var variations = Parser.ParseVariations(entity.Variations, entity.DefaultVariation);

		return new FlagEvaluationOptions(
			evaluationModes,
			schedule,
			operationalWindow,
			targetingRules,
			userAccessControl,
			tenantAccessControl,
			variations);
	}
}

