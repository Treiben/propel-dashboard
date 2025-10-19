using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Propel.FeatureFlags.Dashboard.Api.Security;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token = "", string Username = "", string Role = "", bool ForcePasswordChange = false);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record CreateUserRequest(string Username, string Password, string Role, bool ForcePasswordChange);
public record UpdateUserRequest(string? Role, string? Password, bool? IsActive);
public record UserDto(string Username, string Role, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt);

public sealed class AdminEndpoint : IEndpoint
{
	public void AddEndpoint(IEndpointRouteBuilder epRoutBuilder)
	{
		epRoutBuilder.MapPost("/api/auth/login",
			async (
				LoginRequest request,
				IUserAdministrationService userAdministrationService,
				DashboardConfiguration config,
				CancellationToken cancellationToken) =>
			{
				var user = await userAdministrationService.GetActiveUserAsync(request.Username, cancellationToken);
				if (user is null)
					return Results.Unauthorized();

				var hasher = new PasswordHasher<User>();
				var result = hasher.VerifyHashedPassword(user, user.Password, request.Password);

				if (result != PasswordVerificationResult.Success)
					return Results.Unauthorized();

				// Update last login
				var lastLogin = DateTimeOffset.UtcNow;
				await userAdministrationService.UpdateAsync(username: user.Username, lastLogin: lastLogin, cancellationToken: cancellationToken);

				// Generate JWT
				var token = GenerateJwtToken(user, config);

				return Results.Ok(new LoginResponse(
					Token: token,
					Username: user.Username,
					Role: user.Role,
					ForcePasswordChange: user.ForcePasswordChange));
			})
		.AllowAnonymous()
		.WithName("Login")
		.WithTags("Authorization", "User Login", "Dashboard Api")
		.Produces<LoginResponse>();

		epRoutBuilder.MapPost("/api/auth/password-change/{username}",
			async (
				string username,
				ChangePasswordRequest request,
				IUserAdministrationService userAdministrationService,
				DashboardConfiguration config,
				CancellationToken cancellationToken) =>
			{
				var user = await userAdministrationService.GetActiveUserAsync(username, cancellationToken);
				if (user is null)
					return Results.Unauthorized();

				var hasher = new PasswordHasher<User>();
				var result = hasher.VerifyHashedPassword(user, user.Password, request.CurrentPassword);

				if (result != PasswordVerificationResult.Success)
					return Results.Unauthorized();

				// Update last login
				var lastLogin = DateTimeOffset.UtcNow;
				var updatedUser = await userAdministrationService.UpdateAsync(username: user.Username,
					lastLogin: lastLogin, 
					forcePasswordChange: false,
					password: request.NewPassword,
					cancellationToken: cancellationToken);
			
				// Generate JWT
				var token = GenerateJwtToken(updatedUser, config);

				return Results.Ok(new LoginResponse(
					Token: token,
					Username: updatedUser.Username,
					Role: updatedUser.Role,
					ForcePasswordChange: updatedUser.ForcePasswordChange));
			})
		.RequireAuthorization()
		.WithName("PasswordChange")
		.WithTags("Authorization", "User Login", "Dashboard Api")
		.Produces<LoginResponse>();

		epRoutBuilder.MapGet("/api/admin/users",
			async (IUserAdministrationService userAdministrationService,
				IConfiguration config,
				CancellationToken cancellationToken) =>
			{
				var users = await userAdministrationService.GetAllUsers(cancellationToken);
				return Results.Ok(users);
			})
		.RequireAuthorization("AdminOnly")
		.WithName("GetAllUsers")
		.WithTags("Administration", "Users", "Dashboard Api")
		.Produces<List<User>>();

		epRoutBuilder.MapPost("/api/admin/users",
			async (CreateUserRequest request,
				IUserAdministrationService userAdministrationService,
				IConfiguration config,
				CancellationToken cancellationToken) =>
			{
				if (!UserRole.IsValid(request.Role))
					return Results.BadRequest("Invalid role");

				if (await userAdministrationService.GetActiveUserAsync(request.Username, cancellationToken) is not null)
					return Results.BadRequest("Username already exists");

				var created = await userAdministrationService.CreateUserAsync(
					username: request.Username,
					password: request.Password,
					role: request.Role,
					forcePasswordChange: true,
					cancellationToken);

				return Results.Created($"/api/auth/users/{created.Username}", new UserDto(
					Username: created.Username,
					Role: created.Role,
					IsActive: created.IsActive,
					CreatedAt: created.CreatedAt,
					LastLoginAt: created.CreatedAt));
			})
		.RequireAuthorization("AdminOnly")
		.WithName("CreateUser")
		.WithTags("Administration", "Users", "Dashboard Api")
		.Produces<User>();

		epRoutBuilder.MapPut("/api/admin/users/{username}",
			async (string username,
				UpdateUserRequest request,
				IUserAdministrationService userAdministrationService,
				IConfiguration config,
				CancellationToken cancellationToken) =>
			{
				if (request.Role is not null)
				{
					if (!UserRole.IsValid(request.Role))
						return Results.BadRequest("Invalid role");
				}

				try
				{
					var updated = await userAdministrationService.UpdateAsync(
						username: username,
						password: request.Password,
						role: request.Role,
						isActive: request.IsActive,
						forcePasswordChange: request.Password is not null,
						cancellationToken: cancellationToken);

					return Results.Ok(new UserDto(
						Username: updated.Username,
						Role: updated.Role,
						IsActive: updated.IsActive,
						CreatedAt: updated.CreatedAt,
						LastLoginAt: updated.LastLoginAt));
				}
				catch (InvalidOperationException)
				{
					return Results.NotFound();
				}
			})
		.RequireAuthorization("AdminOnly")
		.WithName("UpdateUser")
		.WithTags("Administration", "Users", "Dashboard Api")
		.Produces<User>();

		epRoutBuilder.MapDelete("/api/admin/users/{username}",
			async (string username,
				IUserAdministrationService userAdministrationService,
				IConfiguration config,
				CancellationToken cancellationToken) =>
			{
				try
				{
					await userAdministrationService.DeleteAsync(username, cancellationToken);
					return Results.NoContent();
				}
				catch (InvalidOperationException)
				{
					return Results.NotFound();
				}
			})
		.RequireAuthorization("AdminOnly")
		.WithName("DeleteUser")
		.WithTags("Administration", "Users", "Dashboard Api")
		.Produces<User>();
	}

	private static string GenerateJwtToken(User user, DashboardConfiguration config)
	{
		var jwtSecret = config.JwtSecret; // Use from PropelConfiguration
		var jwtIssuer = config.JwtIssuer;
		var jwtAudience = config.JwtAudience;

		var key = Encoding.UTF8.GetBytes(jwtSecret);
		var securityKey = new SymmetricSecurityKey(key);
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
		new Claim(JwtRegisteredClaimNames.Sub, user.Username),
		new Claim(JwtRegisteredClaimNames.Name, user.Username),
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		new Claim(ClaimTypes.Role, user.Role),
		new Claim("scope", "propel-dashboard-api"),
		new Claim("scope", "read"),
		new Claim("scope", "write")
	};

		var token = new JwtSecurityToken(
			
			issuer: jwtIssuer,
			audience: jwtAudience,
			claims: claims,
			expires: DateTime.UtcNow.AddDays(1),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
