using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Propel.FeatureFlags.Clients;
using Propel.FeatureFlags.Dashboard.Api.Endpoints;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Shared;
using Propel.FeatureFlags.Dashboard.Api.Infrastructure;

using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.FlagEvaluators;
using Propel.FeatureFlags.Infrastructure;
using Propel.FeatureFlags.Redis;
using Propel.FeatureFlags.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Propel.FeatureFlags.Dashboard.Api;

public static class ProgramExtensions
{
	public static void ConfigureFeatureFlags(this WebApplicationBuilder builder, Action<PropelConfiguration> configure)
	{
		// Configure JSON serialization options for HTTP endpoints (Minimal APIs)
		builder.Services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			options.SerializerOptions.WriteIndented = true;
			options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			options.SerializerOptions.Converters.Add(new EnumJsonConverter<EvaluationMode>());
			options.SerializerOptions.Converters.Add(new EnumJsonConverter<DayOfWeek>());
			options.SerializerOptions.Converters.Add(new EnumJsonConverter<TargetingOperator>());
		});

		// Configure dashboard-specific services
		var propelConfig = builder.Configuration.GetSection("Propel").Get<PropelConfiguration>() ?? new();
		configure.Invoke(propelConfig);

		builder.Services.RegisterEvaluators();

		var cacheOptions = propelConfig.Cache;
		if (cacheOptions.EnableDistributedCache == true)
		{
			builder.Services.AddRedisCache(cacheOptions.Connection);
		}

		builder.Services.AddDatabase(propelConfig);

		builder.Services.AddDashboardServices();

		builder.Services.AddDashboardHealthchecks(propelConfig);
	}

	public static IServiceCollection RegisterEvaluators(this IServiceCollection services)
	{
		// Register evaluation manager with all handlers
		services.AddSingleton<IEvaluatorsSet>(_ => new EvaluatorsSet(
			new HashSet<IEvaluator>(
				[   new ActivationScheduleEvaluator(),
					new OperationalWindowEvaluator(),
					new TargetingRulesEvaluator(),
					new TenantRolloutEvaluator(),
					new TerminalStateEvaluator(),
					new UserRolloutEvaluator(),
				])));
		return services;
	}

	public static IServiceCollection AddDashboardServices(this IServiceCollection services)
	{
		services.TryAddScoped<ICurrentUserService, CurrentUserService>();
		services.TryAddScoped<IFlagResolverService, FlagResolverService>();
		services.TryAddScoped<ICacheInvalidationService, CacheInvalidationService>();

		services.AddValidators();
		services.AddHandlers();
		services.RegisterDashboardEndpoints();
		return services;
	}

	private static IServiceCollection AddValidators(this IServiceCollection services)
	{
		services.AddScoped<IValidator<CreateGlobalFeatureFlagRequest>, CreateFlagRequestValidator>();
		services.AddScoped<IValidator<GetFeatureFlagRequest>, GetFlagsRequestValidator>();
		services.AddScoped<IValidator<ManageTenantAccessRequest>, ManageTenantAccessRequestValidator>();
		services.AddScoped<IValidator<ManageUserAccessRequest>, ManageUserAccessRequestValidator>();
		services.AddScoped<IValidator<UpdateFlagRequest>, UpdateFlagRequestValidator>();
		services.AddScoped<IValidator<UpdateScheduleRequest>, UpdateScheduleRequestValidator>();
		services.AddScoped<IValidator<UpdateTimeWindowRequest>, UpdateTimeWindowRequestValidator>();
		services.AddScoped<IValidator<UpdateTargetingRulesRequest>, UpdateTargetingRulesRequestValidator>();
		services.AddScoped<IValidator<TargetingRuleRequest>, TargetingRuleDtoValidator>();

		return services;
	}

	private static IServiceCollection AddHandlers(this IServiceCollection services)
	{
		services.AddScoped<CreateGlobalFlagHandler>();
		services.AddScoped<DeleteFlagHandler>();
		services.AddScoped<FlagEvaluationHandler>();
		services.AddScoped<GetFilteredFlagsHandler>();
		services.AddScoped<ManageTenantAccessHandler>();
		services.AddScoped<ManageUserAccessHandler>();
		services.AddScoped<SearchFeatureFlagsHandler>();
		services.AddScoped<ToggleFlagHandler>();
		services.AddScoped<UpdateFlagHandler>();
		services.AddScoped<UpdateScheduleHandler>();
		services.AddScoped<UpdateTargetingRulesHandler>();
		services.AddScoped<UpdateTimeWindowHandler>();
		services.AddScoped<UpdateVariationsHandler>();

		return services;
	}

	private static IServiceCollection AddDashboardHealthchecks(this IServiceCollection services, PropelConfiguration propelConfig)
	{
		// Add health checks with proper fallback handling
		var healthChecksBuilder = services.AddHealthChecks();

		// Add liveness check (always available)
		healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy("Application is running"), tags: ["liveness"]);

		// Add PostgreSQL health check only if connection string is available
		var sqlConnection = propelConfig.SqlConnection ?? string.Empty;
		if (!string.IsNullOrEmpty(sqlConnection))
		{
			healthChecksBuilder.AddNpgSql(
				connectionString: sqlConnection,
				healthQuery: "SELECT 1;",
				name: "postgres",
				failureStatus: HealthStatus.Unhealthy,
				tags: ["database", "postgres", "readiness"]);
		}

		// Add Redis health check only if connection string is available
		var redisConnection = propelConfig.Cache.Connection ?? string.Empty;
		if (!string.IsNullOrEmpty(redisConnection))
		{
			healthChecksBuilder.AddRedis(
				redisConnectionString: redisConnection,
				name: "redis",
				failureStatus: HealthStatus.Degraded,
				tags: ["cache", "redis", "readiness"]);
		}

		return services;
	}
}