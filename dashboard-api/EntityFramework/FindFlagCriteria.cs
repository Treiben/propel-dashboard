namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework;

public record class FindFlagCriteria(string? Key = null, string? Name = null, string? Description = null);