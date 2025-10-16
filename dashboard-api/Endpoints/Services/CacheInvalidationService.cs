using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Infrastructure.Cache;

namespace Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;

public interface ICacheInvalidationService
{
	Task InvalidateFlagAsync(FlagIdentifier identifier, CancellationToken cancellationToken);
}

public sealed class CacheInvalidationService(IFeatureFlagCache? cache = null) : ICacheInvalidationService
{
	public async Task InvalidateFlagAsync(FlagIdentifier identifier, CancellationToken cancellationToken)
	{
		if (cache == null) return;

		FlagCacheKey cacheKey = identifier.Scope == Scope.Global
			? new GlobalFlagCacheKey(identifier.Key)
			: new ApplicationFlagCacheKey(identifier.Key, identifier.ApplicationName!, identifier.ApplicationVersion);

		await cache.RemoveAsync(cacheKey, cancellationToken);
	}
}
