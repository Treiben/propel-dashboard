using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Propel.FeatureFlags.Dashboard.Api.Security;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token = "", string UserName = "", string Role = "");
public record CreateUserRequest(string Username, string Password, string Role);
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
				IConfiguration config,
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

				return Results.Ok(new LoginResponse(Token: token, UserName: user.Username, Role: user.Role));
			})
		.AllowAnonymous()
		.WithName("Login")
		.WithTags("Authorization", "User Login", "Dashboard Api")
		.Produces<LoginResponse>();

		epRoutBuilder.MapGet("/api/auth/users",
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

		epRoutBuilder.MapPost("/api/auth/users",
			async (CreateUserRequest request,
				IUserAdministrationService userAdministrationService,
				IConfiguration config,
				CancellationToken cancellationToken) =>
			{
				if (!UserRole.IsValid(request.Role))
					return Results.BadRequest("Invalid role");

				if (await userAdministrationService.GetActiveUserAsync(request.Username, cancellationToken) is not null)
					return Results.BadRequest("Username already exists");

				var created = await userAdministrationService.CreateUserAsync(request.Username, request.Role, request.Password, cancellationToken);

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

		epRoutBuilder.MapPut("/api/auth/users/{username}",
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
						role: request.Role, 
						password: request.Password, 
						isActive: request.IsActive,
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

		epRoutBuilder.MapDelete("/api/auth/users/{username}",
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

	private static string GenerateJwtToken(User user, IConfiguration config)
	{
		var key = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured")));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, user.Username),
			new Claim(ClaimTypes.Name, user.Username),
			new Claim(ClaimTypes.Role, user.Role),
			new Claim("scope", "propel-dashboard-api")
		};

		// Add scope claims based on role
		var scopeClaims = user.Role switch
		{
			UserRole.Admin => ["read", "write", "admin"],
			UserRole.User => ["read", "write"],
			UserRole.Viewer => ["read"],
			_ => Array.Empty<string>()
		};

		var allClaims = claims.Concat(scopeClaims.Select(s => new Claim("scope", s))).ToArray();

		var token = new JwtSecurityToken(
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			claims: allClaims,
			expires: DateTime.UtcNow.AddHours(8),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
