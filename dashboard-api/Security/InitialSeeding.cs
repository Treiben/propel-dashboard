using Microsoft.AspNetCore.Identity;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;

namespace Propel.FeatureFlags.Dashboard.Api.Security;

public static class InitialSeeding
{
    public static async Task SeedDefaultAdminAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IUserAdministrationService>();
        
        // Check if any users exist
        if ((await adminService.GetAllUsers()).Count > 0)
            return;
        
        // Create default admin user - password will be hashed by CreateUserAsync
        await adminService.CreateUserAsync(
            username: "admin", 
            role: UserRole.Admin, 
            password: "Admin123!");  
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Default admin user created. Username: admin, Password: Admin123!");
    }
}
