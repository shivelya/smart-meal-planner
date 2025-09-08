using System.Text;
using Backend.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Backend.Model;
using Backend.Services;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using System.Reflection;
using Backend.Helpers;
using Backend.DTOs;

var builder = WebApplication.CreateBuilder(args);

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
    builder.Host.UseSerilog((ctx, services, lc) =>
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console()
          .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
    });
}

var connectionString = Environment.GetEnvironmentVariable("DOTNET_CONNECTIONSTRING");

if (string.IsNullOrEmpty(connectionString))
{
    // Pull the connection string from configuration (works for both dev & prod)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured in appsettings.json");

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

builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.SupportNonNullableReferenceTypes();

    var subTypes = new List<Type>()
    {
        typeof(ExistingFoodReferenceDto),
        typeof(NewFoodReferenceDto)
    };
    c.SchemaFilter<PolymorphismSchemaFilter<FoodReferenceDto>>(subTypes);

    // Force Swagger to generate schemas for these subtypes
    c.DocumentFilter<EnsureSubTypesDocumentFilter>();
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserService, UserSerivce>();
builder.Services.AddScoped<IEmailService, BrevoEmailService>();
builder.Services.AddScoped<IPantryItemService, PantryItemService>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IRecipeExtractor, ManualRecipeExtractor>();
builder.Services.AddScoped<ISmtpClient, SmtpClientAdapter>();
builder.Services.AddScoped<IRecipeGenerator, ManualRecipeGenerator>();
builder.Services.AddHttpClient<ManualRecipeExtractor>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "An unhandled exception occurred.");

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    });
});

app.MapControllers();
app.Run();