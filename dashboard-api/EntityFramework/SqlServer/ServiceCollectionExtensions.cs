using Microsoft.EntityFrameworkCore;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.SqlServer.Initialization;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.SqlServer;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSqlServerDbContext(this IServiceCollection services, string connectionString)
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

		services.AddDbContext<SqlServerMigrationDbContext>(options =>
		{
			options.UseSqlServer(connectionString, sqlOptions =>
			{
				sqlOptions.MigrationsAssembly(typeof(SqlServerDbContextFactory).Assembly.FullName);
			});
		});

		services.AddScoped<IDashboardRepository, DashboardRepository>();
		return services;
	}
}
