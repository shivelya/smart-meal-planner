using System.Text;
using Backend.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Backend.Model;
using Backend.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Pull the connection string from configuration (works for both dev & prod)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured in appsettings.json");

// Configure logging
// Use Serilog in production, built-in logging in development
if (builder.Environment.IsDevelopment())
{
    // Development: built-in logging (console + debug)
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
    builder.Logging.AddJsonConsole(); // optional structured output
}
else
{
    // Production: use Serilog
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(builder.Configuration) // read settings from appsettings.Production.json
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();
}

builder.Services.AddDbContext<Backend.PlannerContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var rawKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(rawKey))
            throw new InvalidOperationException("JWT Key is not configured in appsettings.json");

        var key = Encoding.UTF8.GetBytes(rawKey);
        options.TokenValidationParameters = new TokenValidationParameters
        { 
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Backend",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SmartMealPlannerUsers",
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // Optional: Set to zero to avoid delay in token expiration
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserService, UserSerivce>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();