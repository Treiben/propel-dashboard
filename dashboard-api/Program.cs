using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Propel.FeatureFlags.Dashboard.Api;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations;
using Propel.FeatureFlags.Dashboard.Api.Healthchecks;
using Propel.FeatureFlags.Dashboard.Api.Security;
using Propel.FeatureFlags.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();

// Configure dashboard-specific services
builder.ConfigureFeatureFlags(options =>
{
	options.Cache = new CacheOptions
	{
		EnableInMemoryCache = false,
		EnableDistributedCache = true,
		Connection = builder.Configuration.GetConnectionString("Redis")!,
	};

	options.SqlConnection = builder.Configuration.GetConnectionString("DefaultConnection")!;
});

// Configure CORS policies
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowFrontend", policy =>
	{
		policy.WithOrigins(
				"http://localhost:3000",
				"https://localhost:3000",
				"http://localhost:5173",
				"https://localhost:5173"
			)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();
	});

	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ??
					throw new InvalidOperationException("JWT secret not configured")))
		};
	});

// Configure authorization policies
builder.Services.AddAuthorizationBuilder()
	.AddPolicy("ApiScope", policy =>
	{
		policy.RequireAuthenticatedUser();
		policy.RequireClaim("scope", "propel-dashboard-api");
	})
	.AddPolicy("AdminOnly", policy =>
	{
		policy.RequireAuthenticatedUser();
		policy.RequireRole("Admin");
	})
	.AddPolicy("CanWrite", policy =>
	{
		policy.RequireAuthenticatedUser();
		policy.RequireClaim("scope", "write");
	})
	.AddFallbackPolicy("RequiresAuth", policy =>
	{
		policy.RequireAuthenticatedUser();
	});

var app = builder.Build();

// Apply database migrations and seed default admin
if (app.Environment.IsDevelopment())
{
	await app.MigrateDatabaseAsync();
	await app.SeedDefaultAdminAsync();
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// CORS must be before Authentication/Authorization
if (app.Environment.IsDevelopment())
{
	app.UseCors("AllowAll");
}
else
{
	app.UseCors("AllowFrontend");
}

app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthCheckEndpoints();
app.MapDashboardEndpoints();

app.Run();
