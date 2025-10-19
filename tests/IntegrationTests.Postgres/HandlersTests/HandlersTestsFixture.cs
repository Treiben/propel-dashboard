using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

using Propel.FeatureFlags.Dashboard.Api;
using Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.PostgreSql;
using Propel.FeatureFlags.Domain;
using Propel.FeatureFlags.Infrastructure.Cache;
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

		var configurationDict = new Dictionary<string, string?>
		{
			["SQL_CONNECTION"] = sqlConnectionString,
			["REDIS_CONNECTION"] = redisConnectionString,
			["ALLOW_FLAGS_UPDATE_IN_REDIS"] = "true",
			["RUN_MIGRATIONS"] = "true",
			["JWT_SECRET"] = "Super secret",
			["JWT_ISSUER"] = "Test",
			["JWT_AUDIENCE"] = "TestAudience",
			["SEED_DEFAULT_ADMIN"] = "true",
			["DEFAULT_ADMIN_USERNAME"] = "admin",
			["DEFAULT_ADMIN_PASSWORD"] = "Admin123!"
		};

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(configurationDict)
			.Build();

		// Configure dashboard-specific services
		var config = DashboardConfiguration.ConfigureProductionSettings(configuration);

		services.AddSingleton(config);

		var mockCurrentUserService = new Mock<ICurrentUserService>();
		mockCurrentUserService.Setup(s => s.Username).Returns("integration-test-user");
		mockCurrentUserService.Setup(s => s.UserId).Returns("integration-test-user-id");

		services.AddSingleton<ICurrentUserService>(mockCurrentUserService.Object);

		services.ConfigureFeatureFlags(config);

		Services = services.BuildServiceProvider();

		// Apply migrations to initialize the database schema
		await ApplyMigrationsAsync();
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
