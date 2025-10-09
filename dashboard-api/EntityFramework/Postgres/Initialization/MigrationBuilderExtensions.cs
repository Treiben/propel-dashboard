using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Postgres.Initialization;

public static class MigrationBuilderExtensions
{
	/// <summary>
	/// Creates a PostgreSQL schema if it doesn't exist.
	/// </summary>
	public static void CreateSchemaIfNotExists(this MigrationBuilder migrationBuilder, string schemaName)
	{
		migrationBuilder.Sql($@"
			DO $$
			BEGIN
				IF NOT EXISTS (
					SELECT schema_name 
					FROM information_schema.schemata 
					WHERE schema_name = '{schemaName}'
				) THEN
					CREATE SCHEMA {schemaName};
				END IF;
			END
			$$;
		");
	}

	public static string? GetSchemaFromConnectionString(string connectionString)
	{
		if (string.IsNullOrEmpty(connectionString))
			return null;

		try
		{
			var builder = new NpgsqlConnectionStringBuilder(connectionString);

			// Npgsql uses "SearchPath" property
			var searchPath = builder.SearchPath;

			// Return the first schema in the search path
			if (!string.IsNullOrEmpty(searchPath))
			{
				var schemas = searchPath.Split(',');
				return schemas[0].Trim();
			}
		}
		catch
		{
			// If parsing fails, return null to use default
		}

		return null;
	}
}