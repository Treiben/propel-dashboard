namespace Propel.FeatureFlags.Dashboard.Api;

public record DashboardConfiguration
{
	public required string SqlConnection { get; init; }
	public required string CacheConnection { get; init; }
	public required bool AllowFlagsUpdateInRedis { get; init; }
	public required bool RunMigrations { get; init; } = true;
	public required bool SeedDefaultAdmin { get; init; } = false;
	public required string DefaultAdminUsername { get; init; } 
	public required string DefaultAdminPassword { get; init; } 
	public required string JwtSecret { get; init; } 
	public required string JwtIssuer { get; init; } 
	public required string JwtAudience { get; init; } 


	public static DashboardConfiguration ConfigureProductionSettings(IConfiguration configuration)
	{
		// Read configuration values from environment variables or appsettings
		var sqlConnectionString = configuration["SQL_CONNECTION"]
			?? configuration.GetConnectionString("SqlConnection");
		var redisConnectionString = configuration["REDIS_CONNECTION"]
			?? configuration.GetConnectionString("RedisConnection");

		// JWT configuration
		var jwtSecret = configuration["JWT_SECRET"]
			?? configuration["Jwt:Secret"];
		// Generate secret if not provided
		if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 10)
			jwtSecret = GenerateRandomSecret();

		var jwtIssuer = configuration["JWT_ISSUER"]
			?? configuration["Jwt:Issuer"];
		var jwtAudience = configuration["JWT_AUDIENCE"]
			?? configuration["Jwt:Audience"];

		// Default admin credentials
		var defaultAdminUsername = configuration["DEFAULT_ADMIN_USERNAME"]
			?? "admin";
		var defaultAdminPassword = configuration["DEFAULT_ADMIN_PASSWORD"]
			?? "Admin123!";

		// Determine if database migrations should be run
		var runMigrations = true;
		var runMigrationsConfig = configuration["RUN_MIGRATIONS"]
			?? configuration["RunMigrations"]
			?? "true";
		if (runMigrationsConfig.Equals("false", StringComparison.OrdinalIgnoreCase)
				|| runMigrationsConfig.Equals("n", StringComparison.OrdinalIgnoreCase)
				|| runMigrationsConfig.Equals("0", StringComparison.OrdinalIgnoreCase))
			runMigrations = false;

		// Determine if default admin seeding is required
		var seedDefaultAdmin = false;
		var seedDefaultAdminConfig = configuration["SEED_DEFAULT_ADMIN"] ?? configuration["SeedDefaultAdmin"] ?? "false";
		if (seedDefaultAdminConfig.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| seedDefaultAdminConfig.Equals("y", StringComparison.OrdinalIgnoreCase)
				|| seedDefaultAdminConfig.Equals("1", StringComparison.OrdinalIgnoreCase))
			seedDefaultAdmin = true;

		// Determine if flags update in Redis is allowed
		var allowFlagsUpdateInRedis = false;
		var allowFlagsUpdateInRedisConfig = configuration["ALLOW_FLAGS_UPDATE_IN_REDIS"]
			?? configuration["AllowFlagsUpdateInRedis"]
			?? "false";
		if (allowFlagsUpdateInRedisConfig.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| allowFlagsUpdateInRedisConfig.Equals("y", StringComparison.OrdinalIgnoreCase)
				|| allowFlagsUpdateInRedisConfig.Equals("1", StringComparison.OrdinalIgnoreCase))
			allowFlagsUpdateInRedis = true;

		return new DashboardConfiguration
		{
			SqlConnection = sqlConnectionString ?? throw new InvalidOperationException("SQL_CONNECTION environment variable or configuration is required"),
			CacheConnection = allowFlagsUpdateInRedis
				? redisConnectionString ?? throw new InvalidOperationException("REDIS_CONNECTION environment variable or configuration is required if ALLOW_FLAGS_UPDATE_IN_REDIS option enabled.")
				: string.Empty,
			AllowFlagsUpdateInRedis = allowFlagsUpdateInRedis,
			RunMigrations = runMigrations,
			SeedDefaultAdmin = seedDefaultAdmin,
			DefaultAdminUsername = defaultAdminUsername,
			DefaultAdminPassword = defaultAdminPassword,
			JwtSecret = jwtSecret ?? throw new InvalidOperationException("JWT_SECRET environment variable or configuration is required"),
			JwtIssuer = jwtIssuer ?? throw new InvalidOperationException("JWT_ISSUER environment variable or configuration is required"),
			JwtAudience = jwtAudience ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable or configuration is required"),
		};
	}

	static string GenerateRandomSecret()
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
		var random = new Random();
		var secret = new string([.. Enumerable.Repeat(chars, 32).Select(s => s[random.Next(s.Length)])]);

		Console.WriteLine($"[WARNING] No JWT_SECRET provided. Generated random secret for this session.");
		Console.WriteLine($"[WARNING] For production use, set JWT_SECRET environment variable.");

		return secret;
	}
}
