using Microsoft.AspNetCore.Authorization;

namespace Propel.FeatureFlags.Dashboard.Api.Endpoints.Shared;

public static class AuthorizationPolicies
{
	public static AuthorizationPolicy CanWrite { get; }
		= new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.RequireClaim("scope", "write", "admin")
		.Build();

	public static AuthorizationPolicy CanRead { get; }
		= new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.RequireClaim("scope", "read", "admin")
		.Build();
}