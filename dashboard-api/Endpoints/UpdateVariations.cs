using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Dto;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Shared;
using Propel.FeatureFlags.Domain;

namespace Propel.FeatureFlags.Dashboard.Api.Endpoints;

public record UpdateVariationsRequest(List<VariationRequest>? Variations, string DefaultVariation, bool RemoveVariations, string? Notes);

public record VariationRequest(string Key, string Value);

public sealed class UpdateVariationsEndpoints : IEndpoint
{
	public void AddEndpoint(IEndpointRouteBuilder epRoutBuilder)
	{
		epRoutBuilder.MapPost("/api/feature-flags/{key}/variations",
			async (
				string key,
				[FromHeader(Name = "X-Scope")] string scope,
				[FromHeader(Name = "X-Application-Name")] string? applicationName,
				[FromHeader(Name = "X-Application-Version")] string? applicationVersion,
				UpdateVariationsRequest request,
				UpdateVariationsHandler handler,
				CancellationToken cancellationToken) =>
			{
				return await handler.HandleAsync(key, new FlagRequestHeaders(scope, applicationName, applicationVersion), 
						request, cancellationToken);
			})
		.RequireAuthorization(AuthorizationPolicies.HasWriteActionPolicy)
		.AddEndpointFilter<ValidationFilter<UpdateVariationsRequest>>()
		.WithName("UpdateVariations")
		.WithTags("Feature Flags", "Operations", "Custom Targeting", "Variations", "Dashboard Api")
		.Produces<FeatureFlagResponse>()
		.ProducesValidationProblem();
	}
}

public sealed class UpdateVariationsHandler(
		IAdministrationService administrationService,
		ICurrentUserService currentUserService,
		ICacheInvalidationService cacheInvalidationService,
		ILogger<UpdateVariationsHandler> logger)
{
	public async Task<IResult> HandleAsync(
		string key,
		FlagRequestHeaders headers,
		UpdateVariationsRequest request,
		CancellationToken cancellationToken)
	{
		try
		{
			var (isValid, result, flag) = await administrationService.ValidateAndResolveFlagAsync(key, headers, cancellationToken);
			if (!isValid) return result;

			var flagWithUpdatedVariations = CreateFlagWithUpdatedVariations(request, flag!);

			var updatedFlag = await administrationService.UpdateAsync(flagWithUpdatedVariations, cancellationToken);
			await cacheInvalidationService.InvalidateFlagAsync(updatedFlag.Identifier, cancellationToken);

			logger.LogInformation("Feature flag {Key} variations updated by {User}",
				key, currentUserService.UserName);

			return Results.Ok(new FeatureFlagResponse(updatedFlag));
		}
		catch (ArgumentException ex)
		{
			return HttpProblemFactory.BadRequest(ex.Message, logger);
		}
		catch (Exception ex)
		{
			return HttpProblemFactory.InternalServerError(ex, logger);
		}
	}

	private FeatureFlag CreateFlagWithUpdatedVariations(UpdateVariationsRequest request, FeatureFlag flag)
	{
		var oldconfig = flag.EvaluationOptions;

		FlagEvaluationOptions configuration;
		FlagAdministration metadata;
		if (request.RemoveVariations || request.Variations == null || request.Variations.Count == 0)
		{
			// Clear existing variations
			configuration = oldconfig with {
				Variations = new Variations() };
			// add to change history
			metadata = flag.Administration with
			{
				ChangeHistory = [.. flag.Administration.ChangeHistory,
					AuditTrail.FlagModified(currentUserService.UserName!, request.Notes ?? "All variations removed")]
			};
		}
		else
		{
			configuration = oldconfig with
			{
				Variations = new Variations {
					Values = request.Variations.ToDictionary(v => v.Key, v => (object)v.Value),
					DefaultVariation = request.DefaultVariation
				}
			};
			// add to change history
			metadata = flag.Administration with
			{
				ChangeHistory = [.. flag.Administration.ChangeHistory,
					AuditTrail.FlagModified(currentUserService.UserName!, request.Notes ?? "Variations updated")]
			};
		}

		return flag with { Administration = metadata, EvaluationOptions = configuration };
	}
}

public sealed class UpdateVariationsRequestValidator : AbstractValidator<UpdateVariationsRequest>
{
	public UpdateVariationsRequestValidator()
	{
		RuleFor(x => x.Notes)
			.MaximumLength(1000)
			.WithMessage("Notes cannot exceed 1000 characters");
	}
}
