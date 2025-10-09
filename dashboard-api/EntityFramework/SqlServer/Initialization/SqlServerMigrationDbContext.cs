using Microsoft.EntityFrameworkCore;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.SqlServer.Initialization;

public class SqlServerMigrationDbContext(DbContextOptions<SqlServerMigrationDbContext> options) : DashboardDbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new FeatureFlagConfiguration());
		modelBuilder.ApplyConfiguration(new FeatureFlagMetadataConfiguration());
		modelBuilder.ApplyConfiguration(new FeatureFlagAuditConfiguration());
	}
}
