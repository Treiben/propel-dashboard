using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.PostgreSql;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.SqlServer;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;

public static class MigrationsProviderExtensions
{
	public static IServiceCollection AddDatabaseMigrationsProvider(this IServiceCollection services, PropelConfiguration config)
	{
		var connectionString = config.SqlConnection
			?? throw new InvalidOperationException("Database connection string is required");

		var databaseProvider = ProviderDetector.DetectProvider(connectionString)
			?? throw new NotSupportedException($"Could not detect database provider from connection string. " +
			$"Supported providers: PostgreSQL, SQL Server");

		return databaseProvider switch
		{
			DatabaseProvider.PostgreSQL => services.AddPostgreSqlMigrations(connectionString),
			DatabaseProvider.SqlServer => services.AddSqlServerMigrations(connectionString),
			_ => throw new NotSupportedException($"Database provider '{databaseProvider}' is not supported")
		};
	}
}
