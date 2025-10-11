using Propel.FeatureFlags.Domain;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework;

public record FeatureFlagFilter(Dictionary<string, string>? Tags = null,
	EvaluationMode[]? EvaluationModes = null,
	string? ApplicationName = null,
	Scope? Scope = null,
	bool? PermanentFlagsOnly = null);

public record class FindFlagCriteria(string? Key = null, string? Name = null, string? Description = null);

public class PagedResult<T>
{
	public List<T> Items { get; set; } = [];
	public int TotalCount { get; set; }
	public int Page { get; set; }
	public int PageSize { get; set; }
	public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
	public bool HasNextPage => Page < TotalPages;
	public bool HasPreviousPage => Page > 1;
}
