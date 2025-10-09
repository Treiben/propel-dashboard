using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.SqlServer;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.SqlServer.Initialization;
using Propel.FeatureFlags.Infrastructure;
using Testcontainers.MsSql;

namespace FeatureFlags.IntegrationTests.SqlServer.SqlServerTests;

public class SqlServerTestsFixture : IAsyncLifetime
{
	private readonly MsSqlContainer _container;
	public IServiceProvider Services { get; private set; } = null!;
	public IFeatureFlagRepository FeatureFlagRepository => Services.GetRequiredService<IFeatureFlagRepository>();
	public IDashboardRepository DashboardRepository => Services.GetRequiredService<IDashboardRepository>();

	public SqlServerTestsFixture()
	{
		_container = new MsSqlBuilder()
			.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
			.WithPassword("StrongP@ssw0rd!")
			.WithEnvironment("ACCEPT_EULA", "Y")
			.WithEnvironment("SA_PASSWORD", "StrongP@ssw0rd!")
			.WithPortBinding(1433, true)
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
		services.AddSqlServerDbContext(connectionString);

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
		using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync();

		// Delete in correct order to respect foreign key constraints
		using var command = new SqlCommand(@"
			TRUNCATE TABLE FeatureFlagsAudit;
			TRUNCATE TABLE FeatureFlagsMetadata;
			TRUNCATE TABLE FeatureFlags;
		", connection);

		await command.ExecuteNonQueryAsync();
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
