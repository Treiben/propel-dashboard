using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres.Initialization;

public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresMigrationDbContext>
{
	public PostgresMigrationDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<PostgresMigrationDbContext>();
		
		// Use a dummy connection string for migrations generation
		// The actual connection string will be used at runtime
		optionsBuilder.UseNpgsql("Host=localhost;Database=propel_feature_flags;Username=propel_user;Password=dummy;", 
			npgsqlOptions =>
			{
				npgsqlOptions.MigrationsAssembly(typeof(PostgresMigrationDbContext).Assembly.FullName);
			});

		return new PostgresMigrationDbContext(optionsBuilder.Options);
	}
}