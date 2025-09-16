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
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IFoodService, FoodService>();
            services.AddScoped<IPantryItemService, PantryItemService>();
            services.AddScoped<IRecipeService, RecipeService>();
            services.AddScoped<IMealPlanService, MealPlanService>();
            services.AddScoped<IMealPlanGenerator, MealPlanGeneratorService>();
            services.AddScoped<IEmailService, BrevoEmailService>();

            services.AddScoped<ISmtpClient, SmtpClientAdapter>();
            services.AddScoped<IRecipeExtractor, ManualRecipeExtractor>();

            services.AddTransient<IExternalRecipeGenerator, SpoonacularRecipeGenerator>();
            services.AddHttpClient<ManualRecipeExtractor>();
            services.AddHttpClient<SpoonacularRecipeGenerator>();

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

            builderAction ??= connectionString =>
            {
                // left this guy strongly typed because Intellisense was having trouble recognizing options
                return (DbContextOptionsBuilder options) => options.UseNpgsql(connectionString);
            }!;

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

        public static IServiceCollection ConfigureSwagger(this IServiceCollection services, IConfiguration config)
        {
            var title = config["Swagger:Title"];
            var version = config["Swagger:Version"];
            var description = config["Swagger:Description"];
            var name = config["Swagger:Contact:Name"];
            var email = config["Swagger:Contact:Email"];
            var url = config["Swagger:Contact:Url"];
            var license = config["Swagger:License:Name"];
            var licUrl = config["Swagger:License:Url"];
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(url) || string.IsNullOrEmpty(license) || string.IsNullOrEmpty(licUrl) )
                throw new InvalidOperationException("Swagger configuration not set up correctly in config file.");

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

                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = title,
                    Version = version,
                    Description = description,
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = name,
                        Email = email,
                        Url = new Uri(url)
                    },
                    License = new Microsoft.OpenApi.Models.OpenApiLicense
                    {
                        Name = license,
                        Url = new Uri(licUrl)
                    }
                });
            });

            return services;
        }
    }
}