using Microsoft.EntityFrameworkCore;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.PostgreSql;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations.SqlServer;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations;

public static class WebApplicationExtensions
{
	public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
	{
		using var scope = app.Services.CreateScope();
		var services = scope.ServiceProvider;
		var logger = services.GetRequiredService<ILogger<Program>>();

		try
		{
			// Try to get PostgresDbContext first
			var postgresMigrationContext = services.GetService<PostgreSqlMigrationDbContext>();
			if (postgresMigrationContext != null)
			{
				logger.LogInformation("Applying PostgreSQL database migrations...");
				await postgresMigrationContext.Database.MigrateAsync();

				// Optionally verify the database was created successfully
				var canConnect = await postgresMigrationContext.Database.CanConnectAsync();
				if (!canConnect)
				{
					throw new InvalidOperationException("Failed to connect to test database after migration.");
				}

				logger.LogInformation("PostgreSQL database migrations applied successfully.");
				return app;
			}

			// Try to get SqlServerDbContext
			var sqlServerContext = services.GetService<SqlServerMigrationDbContext>();
			if (sqlServerContext != null)
			{
				logger.LogInformation("Applying SQL Server database migrations...");
				await sqlServerContext.Database.MigrateAsync();

				// Optionally verify the database was created successfully
				var canConnect = await sqlServerContext.Database.CanConnectAsync();
				if (!canConnect)
				{
					throw new InvalidOperationException("Failed to connect to test database after migration.");
				}

				logger.LogInformation("SQL Server database migrations applied successfully.");
				return app;
			}

			logger.LogWarning("No database context found for migration.");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An error occurred while migrating the database.");
			
			// Decide whether to throw or continue based on your requirements
			// For development, you might want to throw to catch issues early
			// For production, you might want to continue and handle it gracefully
			if (app.Environment.IsDevelopment())
			{
				//throw;
			}
		}

		return app;
	}
}