using Microsoft.EntityFrameworkCore;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Dto;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Shared;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;
using Propel.FeatureFlags.Domain;
using System.Runtime.CompilerServices;

namespace Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;

public interface IAdministrationService
{
	Task<FeatureFlag> CreateAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(FlagIdentifier identifier, string userid, string notes, CancellationToken cancellationToken = default);
	Task<List<FeatureFlag>> FindAsync(FindFlagCriteria criteria, CancellationToken cancellationToken = default);
	Task<bool> FlagExistsAsync(FlagIdentifier identifier, CancellationToken cancellationToken = default);
	Task<List<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<FeatureFlag?> GetByKeyAsync(FlagIdentifier identifier, CancellationToken cancellationToken = default);
	Task<PagedResult<FeatureFlag>> GetPagedAsync(int page, int pageSize, FeatureFlagFilter? filter = null, CancellationToken cancellationToken = default);
	Task<FeatureFlag> UpdateAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
	Task<FeatureFlag> UpdateMetadataAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
	Task<(bool, IResult, FeatureFlag?)> ValidateAndResolveFlagAsync(string key, FlagRequestHeaders headers, CancellationToken cancellationToken);
}

public class AdministrationService(IDatabaseProvider provider, ILogger<AdministrationService> logger) : IAdministrationService
{
	public Task<FeatureFlag> CreateAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
		=> provider.CreateAsync(flag, cancellationToken);

	public Task<bool> DeleteAsync(FlagIdentifier identifier, string userid, string notes, CancellationToken cancellationToken = default)
		=> provider.DeleteAsync(identifier, userid, notes, cancellationToken);

	public async Task<List<FeatureFlag>> FindAsync(FindFlagCriteria criteria, CancellationToken cancellationToken = default)
	{
		var entities = await provider.Context.FeatureFlags
			.AsNoTracking()
			.Include(f => f.Metadata)
			.Include(f => f.AuditTrail)
			.Where(f =>
				!string.IsNullOrWhiteSpace(criteria.Key) && f.Key.ToLower().Equals(criteria.Key.ToLower()) ||
				!string.IsNullOrWhiteSpace(criteria.Name) && f.Name.ToLower().Contains(criteria.Name.ToLower()) ||
				!string.IsNullOrWhiteSpace(criteria.Description) && f.Description.ToLower().Contains(criteria.Description.ToLower()))
			.ToListAsync(cancellationToken);

		if (entities == null)
			return [];

		return [.. entities.Select(Mapper.MapToDomain)];
	}

	public async Task<bool> FlagExistsAsync(FlagIdentifier identifier, CancellationToken cancellationToken = default)
	{
		var entity = await provider.Context.FeatureFlags
			.AsNoTracking()
			.FirstOrDefaultAsync(f =>
				f.Key == identifier.Key &&
				f.ApplicationName == (identifier.ApplicationName ?? "global") &&
				f.ApplicationVersion == (identifier.ApplicationVersion ?? "0.0.0.0") &&
				f.Scope == (int)identifier.Scope, cancellationToken);
		return entity != null;
	}

	public async Task<List<FeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		var entities = await provider.Context.FeatureFlags
						.AsNoTracking()
						.Include(f => f.Metadata)
						.Include(f => f.AuditTrail)
						.OrderBy(f => f.Name)
						.ThenBy(f => f.Key)
						.ToListAsync(cancellationToken);

		return [.. entities.Select(Mapper.MapToDomain)];
	}

	public async Task<FeatureFlag?> GetByKeyAsync(FlagIdentifier identifier, CancellationToken cancellationToken = default)
	{
		var entity = await provider.Context.FeatureFlags
			.AsNoTracking()
			.Include(f => f.Metadata)
			.Include(f => f.AuditTrail)
			.FirstOrDefaultAsync(f =>
				f.Key == identifier.Key &&
				f.ApplicationName == (identifier.ApplicationName ?? "global") &&
				f.ApplicationVersion == (identifier.ApplicationVersion ?? "0.0.0.0") &&
				f.Scope == (int)identifier.Scope,
				cancellationToken);

		if (entity == null)
			return null;

		return Mapper.MapToDomain(entity);
	}

	public async Task<PagedResult<FeatureFlag>> GetPagedAsync(int page, int pageSize,
						FeatureFlagFilter? filter = null, CancellationToken cancellationToken = default)
	{
		// Normalize page parameters
		page = Math.Max(1, page);
		pageSize = Math.Clamp(pageSize, 1, 100);

		var sql = provider.BuildFilterQuery(page, pageSize, filter);
		var countSql = provider.BuildCountQuery(filter);
		var parameters = provider.BuildFilterConditions(filter).parameters;

		// Execute queries
		var entities = await ExecutePagedQuery(sql, parameters, cancellationToken);
		var totalCount = await ExecuteCountQuery(countSql, parameters, cancellationToken);

		return new PagedResult<FeatureFlag>
		{
			Items = [.. entities.Select(Mapper.MapToDomain)],
			TotalCount = totalCount,
			Page = page,
			PageSize = pageSize
		};
	}

	public Task<FeatureFlag> UpdateAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
		=> provider.UpdateAsync(flag, cancellationToken);

	public Task<FeatureFlag> UpdateMetadataAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
		=> provider.UpdateMetadataAsync(flag, cancellationToken);

	public async Task<(bool, IResult, FeatureFlag?)> ValidateAndResolveFlagAsync(string key, FlagRequestHeaders headers, CancellationToken cancellationToken)
	{
		// Validate key parameter
		if (string.IsNullOrWhiteSpace(key))
		{
			return (false, HttpProblemFactory.BadRequest(
				"No feature flag key provided",
				"Feature flag key is required and cannot be empty or null.", logger), null);
		}

		// Validate required scope header
		if (string.IsNullOrWhiteSpace(headers.Scope))
		{
			return (false, HttpProblemFactory.BadRequest(
				"No feature flag scope provided",
				"Feature flag scope is required. Use request header X-Scope: Application for application flags or X-Scope: Global for global flags.", logger), null);
		}

		// Parse scope enum
		if (!Enum.TryParse<Scope>(headers.Scope, true, out var parsedScope))
		{
			return (false, HttpProblemFactory.BadRequest(
				$"Invalid feature flag scope '{headers.Scope}'",
				"Use request header X-Scope: Application for application flags or X-Scope: Global for global flags.", logger), null);
		}

		string application = headers.ApplicationName ?? "";
		string version = headers.ApplicationVersion ?? "1.0.0.0";
		if (parsedScope == Scope.Global)
		{
			application = "global";
			version = "0.0.0.0";
		}

		if (string.IsNullOrWhiteSpace(application))
			return (false, HttpProblemFactory.BadRequest(
				"No application name or version provided",
				"Application name with or without version required for Application scope requests. Pass name and version in headers X-Application-Name, X-Application-Version", logger), null);

		// Resolve flag
		var identifier = new FlagIdentifier(key, parsedScope, application, version);
		var flag = await GetByKeyAsync(identifier, cancellationToken);
		if (flag == null)
		{
			return (false, HttpProblemFactory.NotFound("Feature flag", key, logger), null);
		}

		return (true, Results.Ok(), flag);
	}

	private async Task<List<EntityFramework.Entities.FeatureFlag>> ExecutePagedQuery(string sql, Dictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		// Create FormattableString for FromSqlInterpolated
		var formattableString = CreateFormattableString(sql, parameters);

		return await provider.Context.FeatureFlags
			.FromSqlInterpolated(formattableString)
			.AsNoTracking()
			.Include(f => f.Metadata)
			.Include(f => f.AuditTrail)
			.ToListAsync(cancellationToken);
	}

	private async Task<int> ExecuteCountQuery(string sql, Dictionary<string, object> parameters, CancellationToken cancellationToken)
	{
		var formattableString = CreateFormattableString(sql, parameters);

		// For count queries, we need to execute and get the scalar result differently
		var connection = provider.Context.Database.GetDbConnection();

		await using var command = connection.CreateCommand();
		command.CommandText = formattableString.Format;

		// Add parameters properly
		command.CommandText = string.Format(command.CommandText,
			[.. formattableString.GetArguments().Select(i => $"'{i ?? DBNull.Value}'")]);

		if (connection.State != System.Data.ConnectionState.Open)
			await connection.OpenAsync(cancellationToken);

		var scalarResult = await command.ExecuteScalarAsync(cancellationToken);
		return Convert.ToInt32(scalarResult ?? 0);
	}

	private static FormattableString CreateFormattableString(string sql, Dictionary<string, object> parameters)
	{
		var args = parameters.Values.ToArray();
		var parameterizedSql = sql;

		// Replace parameter placeholders with {0}, {1}, etc.
		int index = 0;
		foreach (var key in parameters.Keys)
		{
			// Handle both @paramName (SQL Server) and {paramName} (PostgreSQL) formats
			parameterizedSql = parameterizedSql.Replace($"@{key}", $"{{{index}}}");
			parameterizedSql = parameterizedSql.Replace($"{{{key}}}", $"{{{index}}}");
			index++;
		}

		return FormattableStringFactory.Create(parameterizedSql, args);
	}
}
