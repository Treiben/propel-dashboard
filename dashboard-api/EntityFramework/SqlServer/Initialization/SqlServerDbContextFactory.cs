using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.SqlServer.Initialization;

/// <summary>
/// Design-time factory for creating SqlServerDbContext instances during migrations.
/// </summary>
public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<SqlServerMigrationDbContext>
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