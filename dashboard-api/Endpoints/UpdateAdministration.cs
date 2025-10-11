using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Dto;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Shared;

namespace Propel.FeatureFlags.Dashboard.Api.Endpoints;

public record UpdateFlagRequest(
	string? Name, 
	string? Description, 
	Dictionary<string, string>? Tags, 
	bool? IsPermanent,
	DateTimeOffset? ExpirationDate, 
	string? Notes);

public sealed class UpdateAdministrationEndpoints : IEndpoint
{
	public void AddEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPut("/api/feature-flags/{key}",
			async (string key,
				[FromHeader(Name = "X-Scope")] string scope,
				[FromHeader(Name = "X-Application-Name")] string? applicationName,
				[FromHeader(Name = "X-Application-Version")] string? applicationVersion,
				UpdateFlagRequest request,
				UpdateFlagHandler handler,
				CancellationToken cancellationToken) =>
			{
				return await handler.HandleAsync(key, new FlagRequestHeaders(scope, applicationName, applicationVersion), request, cancellationToken);
			})
		.AddEndpointFilter<ValidationFilter<UpdateFlagRequest>>()
		.RequireAuthorization(AuthorizationPolicies.HasWriteActionPolicy)
		.WithName("UpdateFeatureFlag")
		.WithTags("Feature Flags", "CRUD Operations", "Update", "Dashboard Api")
		.Produces<FeatureFlagResponse>()
		.ProducesValidationProblem();
	}
}

public sealed class UpdateFlagHandler(
		IAdministrationService administrationService,
		ICurrentUserService currentUserService,
		ICacheInvalidationService cacheInvalidationService,
		ILogger<UpdateFlagHandler> logger)
{
	public async Task<IResult> HandleAsync(string key,
		FlagRequestHeaders headers,
		UpdateFlagRequest request,
		CancellationToken cancellationToken)
	{

		try
		{	
			var (isValid, result, flag) = await administrationService.ValidateAndResolveFlagAsync(key, headers, cancellationToken);
			if (!isValid) return result;

			var flagWithUpdatedMeta = CreateFlagWithUpdatedMetadata(request, flag!, currentUserService.UserName!);

			var updatedFlag = await administrationService.UpdateMetadataAsync(flagWithUpdatedMeta!, cancellationToken);
			await cacheInvalidationService.InvalidateFlagAsync(updatedFlag.Identifier, cancellationToken);

			logger.LogInformation("Feature flag {Key} updated by {User}", key, currentUserService.UserName);
			return Results.Ok(new FeatureFlagResponse(updatedFlag));
		}
		catch (ArgumentException ex)
		{
			return HttpProblemFactory.BadRequest("Invalid argument", ex.Message, logger);
		}
		catch (Exception ex)
		{
			return HttpProblemFactory.InternalServerError(ex, logger);
		}
	}

	private static FeatureFlag CreateFlagWithUpdatedMetadata(UpdateFlagRequest request, FeatureFlag flag, string username)
	{
		// Retention policy
		var isPermanent = request.IsPermanent ?? flag.Administration.RetentionPolicy.IsPermanent;
		var expirationDate = request.ExpirationDate ?? flag.Administration.RetentionPolicy.ExpirationDate;

		// Metadata fields
		var name = string.IsNullOrWhiteSpace(request.Name) ? flag.Administration.Name : request.Name!.Trim();
		var description = string.IsNullOrWhiteSpace(request.Description) ? flag.Administration.Description : request.Description!.Trim();

		var metadata = flag.Administration with
		{
			Name = name,
			Description = description,
			Tags = request.Tags ?? flag.Administration.Tags,
			RetentionPolicy = new RetentionPolicy(isPermanent, expirationDate, new FlagLockPolicy([..flag.EvaluationOptions.ModeSet.Modes])),
			ChangeHistory =
			[
				.. flag.Administration.ChangeHistory,
				AuditTrail.FlagModified(username: username, notes: request.Notes ?? "Flag metadata updated"),
			]
		};

		return flag with { Administration = metadata };
	}
}

public sealed class UpdateFlagRequestValidator : AbstractValidator<UpdateFlagRequest>
{
	public UpdateFlagRequestValidator()
	{
		RuleFor(c => c.Name)
			.MaximumLength(200)
			.When(c => !string.IsNullOrEmpty(c.Name))
			.WithMessage("Feature flag name must be between 1 and 200 characters");

		RuleFor(c => c.Description)
			.MaximumLength(1000)
			.When(c => !string.IsNullOrEmpty(c.Description))
			.WithMessage("Feature flag description cannot exceed 1000 characters");

		RuleFor(c => c.ExpirationDate)
			.GreaterThan(DateTimeOffset.UtcNow)
			.When(c => c.ExpirationDate.HasValue)
			.WithMessage("Expiration date must be in the future");
	}
}