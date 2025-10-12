using System.ComponentModel.DataAnnotations;

namespace Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = UserRole.Viewer;   
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;  
    public DateTimeOffset? LastLoginAt { get; set; }    
    public bool IsActive { get; set; } = true;
}

public static class UserRole
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Viewer = "Viewer";
    
    public static readonly string[] AllRoles = [Admin, User, Viewer];
    
    public static bool IsValid(string role) => AllRoles.Contains(role);
}
