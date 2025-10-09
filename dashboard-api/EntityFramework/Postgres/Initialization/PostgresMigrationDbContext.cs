using Microsoft.EntityFrameworkCore;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres.Initialization;

public class PostgresMigrationDbContext(DbContextOptions<PostgresMigrationDbContext> options) : DashboardDbContext(options)
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new FeatureFlagConfiguration());
		modelBuilder.ApplyConfiguration(new FeatureFlagMetadataConfiguration());
		modelBuilder.ApplyConfiguration(new FeatureFlagAuditConfiguration());
	}
}
