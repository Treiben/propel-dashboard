using Microsoft.EntityFrameworkCore;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Domain;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;

public interface IDatabaseProvider
{
	DashboardDbContext Context { get; }
	Task<FeatureFlag> CreateAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
	Task<FeatureFlag> UpdateAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
	Task<FeatureFlag> UpdateMetadataAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(FlagIdentifier identifier, string userid, string notes, CancellationToken cancellationToken = default);
	string BuildFilterQuery(int page, int pageSize, FeatureFlagFilter filter);
	string BuildCountQuery(FeatureFlagFilter filter);
	(string whereClause, Dictionary<string, object> parameters) BuildFilterConditions(FeatureFlagFilter filter);
}

public abstract class DashboardDbContext(DbContextOptions options) : DbContext(options)
{
	public DbSet<Entities.FeatureFlag> FeatureFlags { get; set; } = null!;
	public DbSet<Entities.FeatureFlagMetadata> FeatureFlagMetadata { get; set; } = null!;
	public DbSet<Entities.FeatureFlagAudit> FeatureFlagAudit { get; set; } = null!;
}
