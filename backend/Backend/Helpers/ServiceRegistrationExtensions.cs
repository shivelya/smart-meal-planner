using System.Reflection;
using System.Text;
using Backend.DTOs;
using Backend.Services;
using Backend.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Helpers
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, BrevoEmailService>();
            services.AddScoped<IPantryItemService, PantryItemService>();
            services.AddScoped<IFoodService, FoodService>();
            services.AddScoped<IRecipeExtractor, ManualRecipeExtractor>();
            services.AddScoped<ISmtpClient, SmtpClientAdapter>();
            services.AddScoped<IRecipeGenerator, ManualRecipeGenerator>();
            services.AddHttpClient<ManualRecipeExtractor>();
            return services;
        }

        // builderAction parameter added to unit testing
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config, Func<string, Action<DbContextOptionsBuilder>>? builderAction = null)
        {
            var connectionString = Environment.GetEnvironmentVariable("DOTNET_CONNECTIONSTRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                // Pull the connection string from configuration (works for both dev & prod)
                connectionString = config.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured in appsettings.json");

            builderAction ??= (conn) => options => options.UseNpgsql(conn);
            services.AddDbContext<PlannerContext>(builderAction(connectionString));

            return services;
        }

        public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var rawKey = config["Jwt:Key"];
                    if (string.IsNullOrEmpty(rawKey))
                        throw new InvalidOperationException("JWT Key is not configured in appsettings.json");

                    var key = Encoding.UTF8.GetBytes(rawKey);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config["Jwt:Issuer"] ?? "Backend",
                        ValidAudience = config["Jwt:Audience"] ?? "SmartMealPlannerUsers",
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero // Optional: Set to zero to avoid delay in token expiration
                    };
                });

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
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

            return services;
        }
    }
}