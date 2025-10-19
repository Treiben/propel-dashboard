using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Infrastructure.Cache;

namespace Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;

public interface ICacheService
{
	Task RemoveFlagAsync(FlagIdentifier identifier, CancellationToken cancellationToken);
	Task UpdateFlagAsync(FeatureFlag flag, CancellationToken cancellationToken);
}

public sealed class CacheService(IFeatureFlagCache? cache = null) : ICacheService
{
	public async Task RemoveFlagAsync(FlagIdentifier identifier, CancellationToken cancellationToken)
	{
		if (cache == null) return;

		FlagCacheKey cacheKey = identifier.Scope == Scope.Global
			? new GlobalFlagCacheKey(identifier.Key)
			: new ApplicationFlagCacheKey(identifier.Key, identifier.ApplicationName!, identifier.ApplicationVersion);

		await cache.RemoveAsync(cacheKey, cancellationToken);
	}

	public async Task UpdateFlagAsync(FeatureFlag flag,  CancellationToken cancellationToken)
	{
		if (cache == null) return;

		FlagCacheKey cacheKey = flag.Identifier.Scope == Scope.Global
			? new GlobalFlagCacheKey(flag.Identifier.Key)
			: new ApplicationFlagCacheKey(flag.Identifier.Key, flag.Identifier.ApplicationName!, flag.Identifier.ApplicationVersion);

		var evaluation = new EvaluationOptions(
			key: flag.Identifier.Key, 
			modeSet: flag.EvaluationOptions.ModeSet, 
			schedule: flag.EvaluationOptions.Schedule,
			targetingRules: flag.EvaluationOptions.TargetingRules, 
			operationalWindow: flag.EvaluationOptions.OperationalWindow,
			userAccessControl: flag.EvaluationOptions.UserAccessControl,
			tenantAccessControl: flag.EvaluationOptions.TenantAccessControl,
			variations: flag.EvaluationOptions.Variations);

		await cache.SetAsync(cacheKey, evaluation, cancellationToken);
	}
}
