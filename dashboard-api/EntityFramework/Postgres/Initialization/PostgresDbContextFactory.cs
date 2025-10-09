using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres.Initialization;

public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresMigrationDbContext>
{
	public PostgresMigrationDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<PostgresMigrationDbContext>();
		
		// Use a connection string with schema for migrations generation
		// The actual connection string will be used at runtime
		optionsBuilder.UseNpgsql(
			"Host=localhost;Database=propel_feature_flags;Search Path=dashboard;Username=propel_user;Password=dummy;", 
			npgsqlOptions =>
			{
				npgsqlOptions.MigrationsAssembly(typeof(PostgresMigrationDbContext).Assembly.FullName);
				// Store migrations history in the same schema
				npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PostgresMigrationDbContext.DefaultSchema);
			});

		return new PostgresMigrationDbContext(optionsBuilder.Options);
	}
}