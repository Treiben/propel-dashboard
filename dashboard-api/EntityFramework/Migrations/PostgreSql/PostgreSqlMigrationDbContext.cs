using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.PostgreSql;

public class PostgreSqlMigrationDbContext(DbContextOptions<PostgreSqlMigrationDbContext> options) : DashboardDbContext(options)
{
	public const string DefaultSchema = "public";

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new PostgreSqlConfigurations.FeatureFlagConfiguration());
		modelBuilder.ApplyConfiguration(new PostgreSqlConfigurations.FeatureFlagMetadataConfiguration());
		modelBuilder.ApplyConfiguration(new PostgreSqlConfigurations.FeatureFlagAuditConfiguration());
		modelBuilder.ApplyConfiguration(new PostgreSqlConfigurations.UserConfiguration());
	}
}

public class PostgreSqlMigrationDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlMigrationDbContext>
{
	public PostgreSqlMigrationDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlMigrationDbContext>();

		// Use a connection string with schema for migrations generation
		// The actual connection string will be used at runtime
		optionsBuilder.UseNpgsql(
			"Host=localhost;Database=propel_feature_flags;Search Path=dashboard;Username=propel_user;Password=dummy;",
			npgsqlOptions =>
			{
				npgsqlOptions.MigrationsAssembly(typeof(PostgreSqlMigrationDbContext).Assembly.FullName);
				// Store migrations history in the same schema
				npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgreSqlMigrationDbContext.DefaultSchema);
			});

		return new PostgreSqlMigrationDbContext(optionsBuilder.Options);
	}
}

public static class PostgreSqlMigrationDbContextExtensions
{
	public static IServiceCollection AddPostgreSqlMigrations(this IServiceCollection services, string connectionString)
	{
		services.AddDbContext<PostgreSqlMigrationDbContext>(options =>
		{
			options.UseNpgsql(connectionString, npgsqlOptions =>
			{
				npgsqlOptions.MigrationsAssembly(typeof(PostgreSqlMigrationDbContextFactory).Assembly.FullName);

				// Extract schema from connection string
				var schema = GetSchemaFromConnectionString(connectionString) ??
					PostgreSqlMigrationDbContext.DefaultSchema;

				// Store migrations history in the schema
				npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);
			});
		});

		return services;
	}

	private static string? GetSchemaFromConnectionString(string connectionString)
	{
		if (string.IsNullOrEmpty(connectionString))
			return null;

		try
		{
			var builder = new NpgsqlConnectionStringBuilder(connectionString);

			// Npgsql uses "SearchPath" property
			var searchPath = builder.SearchPath;

			// Return the first schema in the search path
			if (!string.IsNullOrEmpty(searchPath))
			{
				var schemas = searchPath.Split(',');
				return schemas[0].Trim();
			}
		}
		catch
		{
			// If parsing fails, return null to use default
		}

		return null;
	}
}
