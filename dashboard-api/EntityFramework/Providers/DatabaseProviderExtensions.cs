using Propel.FeatureFlags.Infrastructure;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;

public static class DatabaseProviderExtensions
{
	public static IServiceCollection AddDatabaseProvider(this IServiceCollection services, PropelConfiguration config)
	{
		var databaseProvider = ProviderDetector.DetectProvider(config.SqlConnection)
			?? throw new NotSupportedException($"Could not detect database provider from connection string. " +
			$"Supported providers: PostgreSQL, SQL Server");

		return databaseProvider switch
		{
			DatabaseProvider.PostgreSQL => services.AddPostgreSqlProvider(config.SqlConnection),
			DatabaseProvider.SqlServer => services.AddSqlServerProvider(config.SqlConnection),
			_ => throw new NotSupportedException($"Database provider '{databaseProvider}' is not supported")
		};
	}
}
