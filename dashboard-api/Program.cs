using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Propel.FeatureFlags.Dashboard.Api;
using Propel.FeatureFlags.Dashboard.Api.EntityFramework.Migrations;
using Propel.FeatureFlags.Dashboard.Api.Healthchecks;
using Propel.FeatureFlags.Dashboard.Api.Security;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Override configuration with environment variables
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

var config = PropelConfiguration.ConfigureProductionSettings(builder.Configuration);

builder.Services.AddSingleton(config);

// Configure dashboard-specific services
builder.ConfigureFeatureFlags(config);

// Configure CORS policies
builder.Services.AddCors(options =>
{
	var allowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
		?? ["http://localhost:3000", "http://localhost:5173", "http://localhost:80"];

	options.AddPolicy("AllowFrontend", policy =>
	{
		if (builder.Configuration["CORS_ALLOW_ALL"] == "true")
		{
			policy.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader();
		}
		else
		{
			policy.WithOrigins(allowedOrigins)
				.AllowAnyMethod()
				.AllowAnyHeader()
				.AllowCredentials();
		}
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
			ValidIssuer = config.JwtIssuer ?? "propel-dashboard",
			ValidAudience = config.JwtAudience ?? "propel-dashboard-api",
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.JwtSecret)),
			ClockSkew = TimeSpan.Zero
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
		policy.RequireClaim("scope", "read");
	});

var app = builder.Build();

// Database migration and seeding
if (app.Environment.IsDevelopment())
{
	await app.MigrateDatabaseAsync();
	await app.SeedDefaultAdminAsync(config.DefaultAdminUsername, config.DefaultAdminPassword, forcePasswordChange: false);
}
else
{
	if (config.RunMigrations)
		await app.MigrateDatabaseAsync();

	if (config.SeedDefaultAdmin)
		await app.SeedDefaultAdminAsync(config.DefaultAdminUsername, config.DefaultAdminPassword, forcePasswordChange: true);
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/error");
}

// Only use HTTPS redirection if not in a container
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")))
{
	// Skip HTTPS redirection in containers - handled by reverse proxy
}
else
{
	app.UseHttpsRedirection();
}

// Use appropriate CORS policy
if (app.Environment.IsDevelopment())
{
	app.UseCors("AllowAll");
}
else
{
	app.UseCors("AllowFrontend");
}

app.UseStatusCodePages();

// Serve static files (React frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Swagger configuration
if (app.Environment.IsDevelopment() || builder.Configuration["ENABLE_SWAGGER"] == "true")
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapHealthCheckEndpoints();
app.MapDashboardEndpoints();

// SPA fallback for React Router (serve index.html for non-API routes)
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();