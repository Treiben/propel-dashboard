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

public class ToggleFlagHandlerTests(HandlersTestsFixture fixture)
	: IClassFixture<HandlersTestsFixture>, IAsyncLifetime
{
	[Fact]
	public async Task Should_toggle_flag_on_successfully()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("toggle-on-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Toggle On",
						Description: "Will be enabled",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);


		var handler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ToggleFlagRequest(EvaluationMode.On, "Enabling for production");

		// Act
		var result = await handler.HandleAsync("toggle-on-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
		var response = ((Ok<FeatureFlagResponse>)result).Value;
		response.ShouldNotBeNull();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("toggle-on-flag"), CancellationToken.None);
		updated!.EvaluationOptions.ModeSet.Contains([EvaluationMode.On]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_toggle_flag_off_successfully()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("toggle-off-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Toggle Off",
						Description: "Will be disabled",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);


		var handler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ToggleFlagRequest(EvaluationMode.Off, "Disabling temporarily");

		// Act
		var result = await handler.HandleAsync("toggle-off-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("toggle-off-flag"), CancellationToken.None);
		updated!.EvaluationOptions.ModeSet.Contains([EvaluationMode.Off]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_return_ok_when_flag_already_in_requested_state()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("already-on-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Already On",
						Description: "Already enabled",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ToggleFlagRequest(EvaluationMode.On, "Trying to enable again");

		// Act - First toggle
		await handler.HandleAsync("already-on-flag", headers, request, CancellationToken.None);

		// Act - Second toggle with same state
		var result = await handler.HandleAsync("already-on-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
	}

	[Fact]
	public async Task Should_reset_access_control_when_toggling_on()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("access-control-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Access Control",
						Description: "Will reset access",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ToggleFlagRequest(EvaluationMode.On, "Enabling with full rollout");

		// Act
		var result = await handler.HandleAsync("access-control-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("access-control-flag"), CancellationToken.None);
		updated!.EvaluationOptions.UserAccessControl.RolloutPercentage.ShouldBe(100);
		updated.EvaluationOptions.TenantAccessControl.RolloutPercentage.ShouldBe(100);
	}

	[Fact]
	public async Task Should_reset_access_control_to_zero_when_toggling_off()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("access-off-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Access Off",
						Description: "Will reset to zero",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions with { ModeSet = new ModeSet([EvaluationMode.On]) });

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ToggleFlagRequest(EvaluationMode.Off, "Disabling completely");

		// Act
		var result = await handler.HandleAsync("access-off-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();

		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("access-off-flag"), CancellationToken.None);
		updated!.EvaluationOptions.UserAccessControl.RolloutPercentage.ShouldBe(0);
		updated.EvaluationOptions.TenantAccessControl.RolloutPercentage.ShouldBe(0);
	}

	[Fact]
	public async Task Should_invalidate_cache_after_toggle()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("cached-toggle-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Cached Toggle",
						Description: "In cache",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var cacheKey = new GlobalFlagCacheKey("cached-toggle-flag");
		await fixture.Cache.SetAsync(cacheKey, new EvaluationOptions(key: "cached-toggle-flag"));

		var handler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ToggleFlagRequest(EvaluationMode.On, "Toggle and clear cache");

		// Act
		await handler.HandleAsync("cached-toggle-flag", headers, request, CancellationToken.None);

		// Assert
		var cached = await fixture.Cache.GetAsync(cacheKey);
		cached.ShouldNotBeNull();
		cached.ModeSet.Modes.ShouldContain(EvaluationMode.On);
	}

	[Fact]
	public async Task Should_return_404_when_flag_not_found()
	{
		// Arrange
		var handler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ToggleFlagRequest(EvaluationMode.On, "Toggle non-existent");

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
