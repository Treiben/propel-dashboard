using Knara.UtcStrict;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Dashboard.Api.Endpoints;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Dto;
using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Infrastructure.Cache;

namespace IntegrationTests.SqlServer.HandlersTests;

public class UpdateTargetingRulesHandlerTests(HandlersTestsFixture fixture)
	: IClassFixture<HandlersTestsFixture>, IAsyncLifetime
{
	[Fact]
	public async Task Should_add_targeting_rules_successfully()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("targeting-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Targeting Flag",
						Description: "Will have rules",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<UpdateTargetingRulesHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var rules = new List<TargetingRuleRequest>
		{
			new("country", TargetingOperator.Contains, ["US", "CA"], "variation-a"),
			new("plan", TargetingOperator.Contains, ["premium"], "variation-b")
		};
		var request = new UpdateTargetingRulesRequest(rules, false, "Adding targeting rules");

		// Act
		var result = await handler.HandleAsync("targeting-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("targeting-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TargetingRules.Count.ShouldBe(2);
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TargetingRules]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_remove_all_targeting_rules_successfully()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("remove-targeting-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Remove Targeting",
						Description: "Has rules to remove",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<UpdateTargetingRulesHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);

		// First add rules
		var rules = new List<TargetingRuleRequest>
		{
			new("region", TargetingOperator.Contains, ["EU"], "variation-a")
		};
		await handler.HandleAsync("remove-targeting-flag", headers,
			new UpdateTargetingRulesRequest(rules, false, "Add first"), CancellationToken.None);

		// Act - Remove all rules
		var request = new UpdateTargetingRulesRequest(null, true, "Remove all rules");
		var result = await handler.HandleAsync("remove-targeting-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("remove-targeting-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TargetingRules.ShouldBeEmpty();
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TargetingRules]).ShouldBeFalse();
	}

	[Fact]
	public async Task Should_add_targeting_rules_mode_when_adding_rules()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("mode-targeting-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Mode Targeting",
						Description: "Check mode addition",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<UpdateTargetingRulesHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var rules = new List<TargetingRuleRequest>
		{
			new("tier", TargetingOperator.Contains, ["gold"], "variation-gold")
		};
		var request = new UpdateTargetingRulesRequest(rules, false, "Adding mode");

		// Act
		var result = await handler.HandleAsync("mode-targeting-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("mode-targeting-flag"), CancellationToken.None);
		updated!.EvaluationOptions.ModeSet.Contains([EvaluationMode.TargetingRules]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_remove_on_off_modes_when_adding_targeting_rules()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("cleanup-targeting-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Cleanup Targeting",
						Description: "Remove on/off modes",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<UpdateTargetingRulesHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);

		// First toggle it on
		var toggleHandler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		await toggleHandler.HandleAsync("cleanup-targeting-flag", headers,
			new ToggleFlagRequest(EvaluationMode.On, "Enable first"), CancellationToken.None);

		// Act - Add targeting rules
		var rules = new List<TargetingRuleRequest>
		{
			new("segment", TargetingOperator.Contains, ["beta"], "variation-beta")
		};
		var request = new UpdateTargetingRulesRequest(rules, false, "Add rules");
		var result = await handler.HandleAsync("cleanup-targeting-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("cleanup-targeting-flag"), CancellationToken.None);
		updated!.EvaluationOptions.ModeSet.Contains([EvaluationMode.On]).ShouldBeFalse();
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.Off]).ShouldBeFalse();
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TargetingRules]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_replace_existing_rules_with_new_rules()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("replace-targeting-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Replace Targeting",
						Description: "Replace rules",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<UpdateTargetingRulesHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);

		// Add initial rules
		var initialRules = new List<TargetingRuleRequest>
		{
			new("old-attr", TargetingOperator.Contains, ["old"], "variation-old")
		};
		await handler.HandleAsync("replace-targeting-flag", headers,
			new UpdateTargetingRulesRequest(initialRules, false, "Initial"), CancellationToken.None);

		// Act - Replace with new rules
		var newRules = new List<TargetingRuleRequest>
		{
			new("new-attr", TargetingOperator.Contains, ["new"], "variation-new")
		};
		var request = new UpdateTargetingRulesRequest(newRules, false, "Replace rules");
		var result = await handler.HandleAsync("replace-targeting-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("replace-targeting-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TargetingRules.Count.ShouldBe(1);
		updated.EvaluationOptions.TargetingRules[0].Attribute.ShouldBe("new-attr");
	}

	[Fact]
	public async Task Should_invalidate_cache_after_targeting_rules_update()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("cached-targeting-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Cached Targeting",
						Description: "In cache",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var cacheKey = new GlobalFlagCacheKey("cached-targeting-flag");
		await fixture.Cache.SetAsync(cacheKey, new EvaluationOptions(key: "cached-targeting-flag"));

		var handler = fixture.Services.GetRequiredService<UpdateTargetingRulesHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var rules = new List<TargetingRuleRequest>
		{
			new("env", TargetingOperator.Contains, ["prod"], "variation-prod")
		};
		var request = new UpdateTargetingRulesRequest(rules, false, "Add with cache clear");

		// Act
		await handler.HandleAsync("cached-targeting-flag", headers, request, CancellationToken.None);

		// Assert
		var cached = await fixture.Cache.GetAsync(cacheKey);
		cached.ShouldBeNull();
	}

	[Fact]
	public async Task Should_return_404_when_flag_not_found()
	{
		// Arrange
		var handler = fixture.Services.GetRequiredService<UpdateTargetingRulesHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var rules = new List<TargetingRuleRequest>
		{
			new("attr", TargetingOperator.Contains, ["value"], "variation")
		};
		var request = new UpdateTargetingRulesRequest(rules, false, "Add to non-existent");

		// Act
		var result = await handler.HandleAsync("non-existent", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<ProblemHttpResult>();
		var problem = (ProblemHttpResult)result;
		problem.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
	}

	public Task InitializeAsync() => Task.CompletedTask;
	public Task DisposeAsync() => fixture.ClearAllData();
}

