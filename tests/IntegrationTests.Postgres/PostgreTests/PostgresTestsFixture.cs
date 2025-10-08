﻿using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Propel.FeatureFlags.Dashboard.Api.Infrastructure;
using Propel.FeatureFlags.Dashboard.Api.Infrastructure.Postgres;
using Propel.FeatureFlags.Infrastructure;
using Testcontainers.PostgreSql;

namespace FeatureFlags.IntegrationTests.Postgres.PostgreTests;

public class PostgresTestsFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer _container;
	public IServiceProvider Services {get; private set; } = null!;
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
		var connectionString = await StartContainer();

		var services = new ServiceCollection();

		services.AddLogging();

		services.AddPostgresDbContext(connectionString);

		Services = services.BuildServiceProvider();
	}

	public async Task DisposeAsync()
	{
		await _container.DisposeAsync();
	}

	public async Task ClearAllData()
	{
		var connectionString = _container.GetConnectionString();
		using var connection = new NpgsqlConnection(connectionString);
		await connection.OpenAsync();
		using var command = new NpgsqlCommand(@"
			DELETE FROM feature_flags_audit;
			DELETE FROM feature_flags_metadata;
			DELETE FROM feature_flags;
		", connection);
		await command.ExecuteNonQueryAsync();
	}

	private async Task<string> StartContainer()
	{
		await _container.StartAsync();

		var connectionString = _container.GetConnectionString();
		return connectionString;
	}
}
