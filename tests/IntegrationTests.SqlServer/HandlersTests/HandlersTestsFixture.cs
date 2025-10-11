using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Propel.FeatureFlags.Dashboard.Api;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.SqlServer;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;
using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Infrastructure;
using Propel.FeatureFlags.Infrastructure.Cache;
using Propel.FeatureFlags.Redis;
using Propel.FeatureFlags.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace IntegrationTests.SqlServer.HandlersTests;

public class HandlersTestsFixture : IAsyncLifetime
{
	private readonly MsSqlContainer _sqlContainer;
	private readonly RedisContainer _redisContainer;

	public IServiceProvider Services {get; private set; } = null!;
	public IAdministrationService AdministrationService => Services.GetRequiredService<IAdministrationService>();

	public IFeatureFlagCache Cache => Services.GetRequiredService<IFeatureFlagCache>();

	public HandlersTestsFixture()
	{
		_sqlContainer = new MsSqlBuilder()
			.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
			.WithPassword("StrongP@ssw0rd!")
			.WithEnvironment("ACCEPT_EULA", "Y")
			.WithEnvironment("SA_PASSWORD", "StrongP@ssw0rd!")
			.WithPortBinding(1433, true)
			.Build();

		_redisContainer = new RedisBuilder()
			.WithImage("redis:7-alpine")
			.WithPortBinding(6379, true)
			.Build();
	}

	public async Task InitializeAsync()
	{
		await _sqlContainer.StartAsync();
		await _redisContainer.StartAsync();

		var sqlConnectionString = _sqlContainer.GetConnectionString();
		var redisConnectionString = _redisContainer.GetConnectionString();

		var services = new ServiceCollection();
		services.AddLogging();

		services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			options.SerializerOptions.WriteIndented = true;
			options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			options.SerializerOptions.Converters.Add(new EnumJsonConverter<EvaluationMode>());
			options.SerializerOptions.Converters.Add(new EnumJsonConverter<DayOfWeek>());
			options.SerializerOptions.Converters.Add(new EnumJsonConverter<TargetingOperator>());
		});

		ConfigureFeatureFlags(services, options =>
		{
			options.SqlConnection = sqlConnectionString;
			options.Cache = new CacheOptions
			{
				EnableInMemoryCache = false,
				EnableDistributedCache = true,
				Connection = _redisContainer.GetConnectionString()
			};
		});
		Services = services.BuildServiceProvider();
		// Apply migrations to initialize the database schema
		await ApplyMigrationsAsync();
	}

	public void ConfigureFeatureFlags(IServiceCollection services, Action<PropelConfiguration> configure)
	{
		PropelConfiguration propelConfig = new();
		configure.Invoke(propelConfig);

		services.AddSingleton(propelConfig);

		services.RegisterEvaluators();

		var cacheOptions = propelConfig.Cache;
		if (cacheOptions.EnableDistributedCache == true)
		{
			services.AddRedisCache(cacheOptions.Connection);
		}

		var mockCurrentUserService = new Mock<ICurrentUserService>();
		mockCurrentUserService.Setup(s => s.UserName).Returns("integration-test-user");
		mockCurrentUserService.Setup(s => s.UserId).Returns("integration-test-user-id");

		services.AddSingleton<ICurrentUserService>(mockCurrentUserService.Object);

		services.AddDatabaseProvider(propelConfig)
				.AddDashboardServices();

		services.AddDatabaseMigrationsProvider(propelConfig);
	}

	public async Task DisposeAsync()
	{
		await _sqlContainer.DisposeAsync();
		await _redisContainer.DisposeAsync();
	}

	public async Task ClearAllData()
	{
		var connectionString = _sqlContainer.GetConnectionString();
		using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync();
		using var command = new SqlCommand("DELETE FROM FeatureFlags", connection);
		await command.ExecuteNonQueryAsync();

		await Cache.ClearAsync();
	}

	private async Task ApplyMigrationsAsync()
	{
		using var scope = Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<SqlServerMigrationDbContext>();

		// Apply all pending migrations
		await dbContext.Database.MigrateAsync();

		// Optionally verify the database was created successfully
		var canConnect = await dbContext.Database.CanConnectAsync();
		if (!canConnect)
		{
			throw new InvalidOperationException("Failed to connect to test database after migration.");
		}
	}
}
