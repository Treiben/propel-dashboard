using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.SqlServer;
using Propel.FeatureFlags.Infrastructure;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDatabase(this IServiceCollection services, PropelConfiguration config)
	{
		var connectionString = config.SqlConnection
			?? throw new InvalidOperationException("Database connection string is required");

		var databaseProvider = ConnectionStringHelper.DetectProvider(connectionString)
			?? throw new NotSupportedException($"Could not detect database provider from connection string. " +
			$"Supported providers: PostgreSQL, SQL Server"); 

		return databaseProvider switch
		{
			DatabaseProvider.PostgreSQL => services.AddPostgresDbContext(connectionString),
			DatabaseProvider.SqlServer => services.AddSqlServerDbContext(connectionString),
			_ => throw new NotSupportedException($"Database provider '{databaseProvider}' is not supported")
		};
	}
}
