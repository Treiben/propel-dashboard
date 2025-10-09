using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres.Initialization;
using Propel.FeatureFlags.Infrastructure;
using Testcontainers.PostgreSql;

namespace FeatureFlags.IntegrationTests.Postgres.PostgreTests;

public class PostgresTestsFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer _container;
	public IServiceProvider Services { get; private set; } = null!;
	public IFeatureFlagRepository FeatureFlagRepository => Services.GetRequiredService<IFeatureFlagRepository>();
	public IDashboardRepository DashboardRepository => Services.GetRequiredService<IDashboardRepository>();

	public PostgresTestsFixture()
	{
		_container = new PostgreSqlBuilder()
			.WithImage("postgres:15-alpine")
			.WithDatabase("feature_flags_test")
			.WithUsername("test_user")
			.WithPassword("test_password")
			.WithPortBinding(5432, true)
			.Build();
	}

	public async Task InitializeAsync()
	{
		// Start the container
		await _container.StartAsync();
		
		var connectionString = _container.GetConnectionString();

		var services = new ServiceCollection();

		services.AddLogging();

		// Add the DbContext with test container connection string
		services.AddPostgresDbContext(connectionString);

		Services = services.BuildServiceProvider();

		// Apply migrations to initialize the database schema
		await ApplyMigrationsAsync();
	}

	public async Task DisposeAsync()
	{
		if (Services is IDisposable disposable)
		{
			disposable.Dispose();
		}
		
		await _container.DisposeAsync();
	}

	public async Task ClearAllData()
	{
		var connectionString = _container.GetConnectionString();
		using var connection = new NpgsqlConnection(connectionString);
		await connection.OpenAsync();
		
		// Delete in correct order to respect foreign key constraints
		using var command = new NpgsqlCommand(@"
			TRUNCATE TABLE feature_flags_audit CASCADE;
			TRUNCATE TABLE feature_flags_metadata CASCADE;
			TRUNCATE TABLE feature_flags CASCADE;
		", connection);
		
		await command.ExecuteNonQueryAsync();
	}

	private async Task ApplyMigrationsAsync()
	{
		using var scope = Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<PostgresMigrationDbContext>();
		
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
