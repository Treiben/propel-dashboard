using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;

namespace Propel.FeatureFlags.Dashboard.Api.Security;

public static class DatabaseSeederExtensions
{
	public static async Task SeedDefaultAdminAsync(this WebApplication app,
		string username = "admin",
		string password = "admin1234!",
		bool forcePasswordChange = false)
	{
		using var scope = app.Services.CreateScope();
		var adminService = scope.ServiceProvider.GetRequiredService<IUserAdministrationService>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

		try
		{
			// Check if admin user already exists
			var users = await adminService.GetAllUsers();
			var adminExists = users.Any(u => u.Username == username);

			if (!adminExists)
			{
				await adminService.CreateUserAsync(
							username: username,
							role: UserRole.Admin,
							password: password,
							forcePasswordChange: forcePasswordChange);

				logger.LogInformation("Default admin user '{Username}' created successfully", username);

				if (forcePasswordChange)
				{
					logger.LogWarning("Admin user must change password on first login");
				}
			}
			else
			{
				logger.LogInformation("Default admin user '{Username}' already exists", username);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to seed default admin user");
			// Don't throw - this shouldn't prevent app startup
		}
	}
}
