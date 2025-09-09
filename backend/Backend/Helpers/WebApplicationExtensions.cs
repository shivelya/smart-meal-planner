using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace Backend.Helpers
{
    public static class WebApplicationExtensions
    {
        public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
        {
            // Use Serilog in production, built-in logging in development
            if (builder.Environment.IsDevelopment())
            {
                // Development: built-in logging (console + debug)
                builder.Logging.ClearProviders();
                builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
                builder.Logging.AddDebug();
                builder.Logging.AddJsonConsole(); // optional structured output
            }
            else
            {
                // Production: use Serilog
                builder.Host.UseSerilog((ctx, services, lc) =>
                    lc.ReadFrom.Configuration(ctx.Configuration) .Enrich.FromLogContext()
                );
            }

            return builder;
        }

        public static WebApplication ConfigureExceptionHandling(this WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    var factory = app.Services.GetRequiredService<ILoggerFactory>();
                    var logger = factory.CreateLogger("WebApplicationExtensions");
                    logger.LogError(exception, "An unhandled exception occurred.");

                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
                });
            });

            return app;
        }

        public static WebApplication UseMyAppPipeline(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }
}