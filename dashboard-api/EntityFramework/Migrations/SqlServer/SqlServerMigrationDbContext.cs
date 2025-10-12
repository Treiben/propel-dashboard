using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.SqlServer;

public class SqlServerMigrationDbContext(DbContextOptions<SqlServerMigrationDbContext> options) : DashboardDbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.FeatureFlagConfiguration());
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.FeatureFlagMetadataConfiguration());
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.FeatureFlagAuditConfiguration());
		modelBuilder.ApplyConfiguration(new SqlServerConfigurations.UserConfiguration());
	}
}

public class SqlServerMigrationDbContextFactory : IDesignTimeDbContextFactory<SqlServerMigrationDbContext>
{
	public SqlServerMigrationDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<SqlServerMigrationDbContext>();

		// Use a dummy connection string for migrations generation
		// The actual connection string will be used at runtime
		optionsBuilder.UseSqlServer("Server=localhost;Database=PropelFeatureFlags;Integrated Security=true;TrustServerCertificate=true;",
			sqlOptions =>
			{
				sqlOptions.MigrationsAssembly(typeof(SqlServerMigrationDbContext).Assembly.FullName);
			});

		return new SqlServerMigrationDbContext(optionsBuilder.Options);
	}
}

public static class SqlServerMigrationDbContextExtensions
{
	public static IServiceCollection AddSqlServerMigrations(this IServiceCollection services, string connectionString)
	{
		services.AddDbContext<SqlServerMigrationDbContext>(options =>
		{
			options.UseSqlServer(connectionString, sqlOptions =>
			{
				sqlOptions.MigrationsAssembly(typeof(SqlServerMigrationDbContextFactory).Assembly.FullName);
			});
		});

		return services;
	}
}
