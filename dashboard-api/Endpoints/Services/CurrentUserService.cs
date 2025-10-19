using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Propel.FeatureFlags.Dashboard.Api.Endpoints.Services;

public interface ICurrentUserService
{
	string? Username { get; }
}

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
	private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
	public string? Username =>
		_httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Name) ??
		_httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
		_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ??
		_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
		"system";
}
