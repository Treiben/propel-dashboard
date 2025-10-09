using Microsoft.EntityFrameworkCore;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres.Initialization;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPostgresDbContext(this IServiceCollection services, string connectionString)
	{		
		services.AddDbContext<PostgresDbContext>(options =>
		{
			options.UseNpgsql(connectionString, npgsqlOptions =>
			{
				npgsqlOptions.EnableRetryOnFailure(
					maxRetryCount: 3,
					maxRetryDelay: TimeSpan.FromSeconds(5),
					errorCodesToAdd: null);
			});
			// Configure for development/production
			options.EnableSensitiveDataLogging(false);
			options.EnableDetailedErrors(false);
		});

		services.AddDbContext<PostgresMigrationDbContext>(options =>
		{
			options.UseNpgsql(connectionString, npgsqlOptions =>
			{
				npgsqlOptions.MigrationsAssembly(typeof(PostgresDbContextFactory).Assembly.FullName);

				// Extract schema from connection string
				var schema = MigrationBuilderExtensions.GetSchemaFromConnectionString(connectionString) ??
					PostgresMigrationDbContext.DefaultSchema;

				// Store migrations history in the schema
				npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);
			});
		});

		services.AddScoped<IDashboardRepository, DashboardRepository>();
		return services;
	}
}