using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Entities;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Providers;

namespace Propel.FeatureFlags.Dashboard.Api.Security;

public interface IUserAdministrationService
{
	Task<User> CreateUserAsync(string username, string role, string password, bool forcePasswordChange = true, CancellationToken cancellationToken = default);
	Task DeleteAsync(string username, CancellationToken cancellationToken = default);
	Task<User?> GetActiveUserAsync(string username, CancellationToken cancellationToken = default);
	Task<List<User>> GetAllUsers(CancellationToken cancellationToken = default);
	Task<User> UpdateAsync(string username, string? role = null, string? password = null, bool? isActive = null, DateTimeOffset? lastLogin = null, CancellationToken cancellationToken = default);
}


public sealed class UserAdministrationService(IDatabaseProvider provider) : IUserAdministrationService
{
	public async Task<User> CreateUserAsync(string username, 
											string role, 
											string password,
											bool forcePasswordChange = true,
											CancellationToken cancellationToken = default)
	{
		var hasher = new PasswordHasher<User>();
		var user = new User
		{
			Username = username,
			Role = role,
			IsActive = true,
			ForcePasswordChange = forcePasswordChange
		};
		user.Password = hasher.HashPassword(user, password);

		provider.Context.Users.Add(user);
		await provider.Context.SaveChangesAsync(cancellationToken);

		return user;
	}

	public async Task DeleteAsync(string username, CancellationToken cancellationToken = default)
	{
		try
		{
			var user = await provider.Context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken: cancellationToken)
				?? throw new InvalidOperationException("User not found");
			provider.Context.Users.Remove(user);
			await provider.Context.SaveChangesAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Error deleting user", ex);
		}
	}

	public async Task<User?> GetActiveUserAsync(string username, CancellationToken cancellationToken = default) =>
		await provider.Context.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken: cancellationToken);

	public async Task<List<User>> GetAllUsers(CancellationToken cancellationToken) => await provider.Context.Users
														.AsNoTracking()
														.ToListAsync(cancellationToken: cancellationToken);
	public async Task<User> UpdateAsync(string username, 
		string? role = null, 
		string? password = null, 
		bool? isActive = null, 
		DateTimeOffset? lastLogin = null,
		CancellationToken cancellationToken = default)
	{
		var user = await provider.Context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken: cancellationToken) ?? throw new InvalidOperationException("User not found");
		if (role is not null)
			user.Role = role;
		if (isActive is not null)
			user.IsActive = isActive.Value;
		if (password is not null)
		{
			var hasher = new PasswordHasher<User>();
			user.Password = hasher.HashPassword(user, password);
		}
		if (lastLogin is not null)
			user.LastLoginAt = lastLogin;

		provider.Context.Users.Update(user);
		await provider.Context.SaveChangesAsync(cancellationToken);

		return user;
	}
}