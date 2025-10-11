using Propel.FeatureFlags.Infrastructure;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework;

public static class ProviderDetector
{
	public static DatabaseProvider? DetectProvider(string connectionString)
	{
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));
		}

		var lowerConnectionString = connectionString.ToLowerInvariant();

		// PostgreSQL patterns
		if (lowerConnectionString.StartsWith("postgres://") ||
			lowerConnectionString.StartsWith("postgresql://") ||
			lowerConnectionString.Contains("host=") ||
			lowerConnectionString.Contains("server=") && lowerConnectionString.Contains("port=5432"))
		{
			return DatabaseProvider.PostgreSQL;
		}

		// SQL Server patterns
		if (lowerConnectionString.Contains("data source=") ||
			lowerConnectionString.Contains("server=") ||
			lowerConnectionString.Contains("initial catalog=") ||
			lowerConnectionString.Contains("database=") ||
			lowerConnectionString.Contains("integrated security=") ||
			lowerConnectionString.Contains("trusted_connection="))
		{
			return DatabaseProvider.SqlServer;
		}
		return null;
	}

	private static DatabaseProvider? DetectProviderFromPort(int? port)
	{
		return port switch
		{
			5432 => DatabaseProvider.PostgreSQL,
			1433 => DatabaseProvider.SqlServer,
			_ => null
		};
	}
}
