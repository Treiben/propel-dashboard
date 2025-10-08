﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

using Propel.FeatureFlags.Dashboard.Api.Infrastructure;
using Propel.FeatureFlags.Dashboard.Api.Infrastructure.SqlServer;
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
		var connectionString = await StartContainer();

		var services = new ServiceCollection();

		services.AddLogging();

		services.AddSqlServerDbContext(connectionString);

		Services = services.BuildServiceProvider();
	}

	public async Task DisposeAsync()
	{
		await _container.DisposeAsync();
	}

	public async Task ClearAllData()
	{
		var connectionString = _container.GetConnectionString();
		using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync();
		using var command = new SqlCommand(@"
			DELETE FROM FeatureFlagsAudit;
			DELETE FROM FeatureFlagsMetadata;
			DELETE FROM FeatureFlags;", connection);
		await command.ExecuteNonQueryAsync();
	}

	private async Task<string> StartContainer()
	{
		await _container.StartAsync();

		var connectionString = _container.GetConnectionString();
		return connectionString;
	}
}
