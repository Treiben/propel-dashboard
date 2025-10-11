using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

using Propel.FeatureFlags.Dashboard.Api;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.PostgreSql;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;
using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Infrastructure;
using Propel.FeatureFlags.Infrastructure.Cache;
using Propel.FeatureFlags.Redis;
using Propel.FeatureFlags.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;

using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace IntegrationTests.Postgres.HandlersTests;

public class HandlersTestsFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer _postgresContainer;
	private readonly RedisContainer _redisContainer;

	public IServiceProvider Services {get; private set; } = null!;
	public IAdministrationService AdministrationService => Services.GetRequiredService<IAdministrationService>();

	public IFeatureFlagCache Cache => Services.GetRequiredService<IFeatureFlagCache>();

	public HandlersTestsFixture()
	{
		_postgresContainer = new PostgreSqlBuilder()
			.WithImage("postgres:15-alpine")
			.WithDatabase("featureflags_client")
			.WithUsername("test_user")
			.WithPassword("test_password")
			.WithPortBinding(5432, true)
			.Build();

		_redisContainer = new RedisBuilder()
			.WithImage("redis:7-alpine")
			.WithPortBinding(6379, true)
			.Build();
	}

	public async Task InitializeAsync()
	{
		await _postgresContainer.StartAsync();
		await _redisContainer.StartAsync();

		var sqlConnectionString = _postgresContainer.GetConnectionString();
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
		await _postgresContainer.DisposeAsync();
		await _redisContainer.DisposeAsync();
	}

	public async Task ClearAllData()
	{
		var connectionString = _postgresContainer.GetConnectionString();
		using var connection = new NpgsqlConnection(connectionString);
		using var command = new NpgsqlCommand(@"
			TRUNCATE TABLE feature_flags_audit CASCADE;
			TRUNCATE TABLE feature_flags_metadata CASCADE;
			TRUNCATE TABLE feature_flags CASCADE;
		", connection);

		await connection.OpenAsync();
		await command.ExecuteNonQueryAsync();

		await Cache.ClearAsync();
	}

	private async Task ApplyMigrationsAsync()
	{
		using var scope = Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<PostgreSqlMigrationDbContext>();

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
