using Knara.UtcStrict;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Propel.FeatureFlags.Dashboard.Api.Domain;
using Propel.FeatureFlags.Dashboard.Api.Endpoints;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Dto;
using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Infrastructure.Cache;

namespace IntegrationTests.Postgres.HandlersTests;

public class ManageTenantAccessHandlerTests(HandlersTestsFixture fixture)
	: IClassFixture<HandlersTestsFixture>, IAsyncLifetime
{
	[Fact]
	public async Task Should_set_tenant_rollout_percentage_successfully()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("tenant-percentage-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Tenant Percentage",
						Description: "Will have percentage",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ManageTenantAccessRequest(null, null, 75, "Set 75% rollout");

		// Act
		var result = await handler.HandleAsync("tenant-percentage-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
		
		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("tenant-percentage-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TenantAccessControl.RolloutPercentage.ShouldBe(75);
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TenantRolloutPercentage]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_add_allowed_tenants_successfully()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("allowed-tenants-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Allowed Tenants",
						Description: "Will have allowed tenants",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ManageTenantAccessRequest(
			["tenant-1", "tenant-2"], 
			null, 
			null, 
			"Adding allowed tenants");

		// Act
		var result = await handler.HandleAsync("allowed-tenants-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
		
		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("allowed-tenants-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TenantAccessControl.Allowed.ShouldContain("tenant-1");
		updated.EvaluationOptions.TenantAccessControl.Allowed.ShouldContain("tenant-2");
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TenantTargeted]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_add_blocked_tenants_successfully()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("blocked-tenants-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Blocked Tenants",
						Description: "Will have blocked tenants",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ManageTenantAccessRequest(
			null, 
			["tenant-bad-1", "tenant-bad-2"], 
			null, 
			"Blocking tenants");

		// Act
		var result = await handler.HandleAsync("blocked-tenants-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
		
		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("blocked-tenants-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TenantAccessControl.Blocked.ShouldContain("tenant-bad-1");
		updated.EvaluationOptions.TenantAccessControl.Blocked.ShouldContain("tenant-bad-2");
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TenantTargeted]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_remove_rollout_mode_when_percentage_is_one_hundred()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("hundred-percentage-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Hundred Percentage",
						Description: "Test unrestricted rollout",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ManageTenantAccessRequest(null, null, 100, "Set to 100%");

		// Act
		var result = await handler.HandleAsync("hundred-percentage-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
		
		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("hundred-percentage-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TenantAccessControl.RolloutPercentage.ShouldBe(100);
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TenantRolloutPercentage]).ShouldBeFalse();
	}

	[Fact]
	public async Task Should_remove_on_off_modes_when_setting_tenant_access()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("tenant-mode-cleanup-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Tenant Mode Cleanup",
						Description: "Remove on/off modes",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		
		// First toggle it on
		var toggleHandler = fixture.Services.GetRequiredService<ToggleFlagHandler>();
		await toggleHandler.HandleAsync("tenant-mode-cleanup-flag", headers, 
			new ToggleFlagRequest(EvaluationMode.On, "Enable first"), CancellationToken.None);

		// Act - Set tenant access
		var request = new ManageTenantAccessRequest(null, null, 50, "Set tenant access");
		var result = await handler.HandleAsync("tenant-mode-cleanup-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
		
		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("tenant-mode-cleanup-flag"), CancellationToken.None);
		updated!.EvaluationOptions.ModeSet.Contains([EvaluationMode.On]).ShouldBeFalse();
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.Off]).ShouldBeFalse();
	}

	[Fact]
	public async Task Should_set_both_allowed_and_percentage_together()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("combined-tenant-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Combined Tenant",
						Description: "Both allowed and percentage",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ManageTenantAccessRequest(
			["tenant-a"], 
			null, 
			80, 
			"Combined settings");

		// Act
		var result = await handler.HandleAsync("combined-tenant-flag", headers, request, CancellationToken.None);

		// Assert
		result.ShouldBeOfType<Ok<FeatureFlagResponse>>();
		
		var updated = await fixture.AdministrationService.GetByKeyAsync(
			new GlobalFlagIdentifier("combined-tenant-flag"), CancellationToken.None);
		updated!.EvaluationOptions.TenantAccessControl.Allowed.ShouldContain("tenant-a");
		updated.EvaluationOptions.TenantAccessControl.RolloutPercentage.ShouldBe(80);
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TenantTargeted]).ShouldBeTrue();
		updated.EvaluationOptions.ModeSet.Contains([EvaluationMode.TenantRolloutPercentage]).ShouldBeTrue();
	}

	[Fact]
	public async Task Should_invalidate_cache_after_tenant_access_update()
	{
		// Arrange
		var identifier = new GlobalFlagIdentifier("cached-tenant-flag");
		var flag = new FeatureFlag(identifier,
			new FlagAdministration(Name: "Cached Tenant",
						Description: "In cache",
						RetentionPolicy: new RetentionPolicy(IsPermanent: true, ExpirationDate: UtcDateTime.MaxValue, new FlagLockPolicy([EvaluationMode.On])),
						Tags: [],
						ChangeHistory: [AuditTrail.FlagCreated("test-user", null)]),
			FlagEvaluationOptions.DefaultOptions);

		await fixture.AdministrationService.CreateAsync(flag, CancellationToken.None);

		var cacheKey = new GlobalFlagCacheKey("cached-tenant-flag");
		await fixture.Cache.SetAsync(cacheKey, new EvaluationOptions(key: "cached-tenant-flag"));

		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ManageTenantAccessRequest(null, null, 60, "Update with cache clear");

		// Act
		await handler.HandleAsync("cached-tenant-flag", headers, request, CancellationToken.None);

		// Assert
		var cached = await fixture.Cache.GetAsync(cacheKey);
		cached.ShouldNotBeNull();
		cached.TenantAccessControl.RolloutPercentage.ShouldBe(60);
	}

	[Fact]
	public async Task Should_return_404_when_flag_not_found()
	{
		// Arrange
		var handler = fixture.Services.GetRequiredService<ManageTenantAccessHandler>();
		var headers = new FlagRequestHeaders("Global", null, null);
		var request = new ManageTenantAccessRequest(null, null, 50, "Non-existent flag");

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
