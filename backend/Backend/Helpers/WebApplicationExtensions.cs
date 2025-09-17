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
                    lc.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext()
                );
            }

            return builder;
        }

        public static WebApplication ConfigureExceptionHandling(this WebApplication app)
        {
            // ensures all exceptions are handled so nothing sensitive is exposed to the user and connections don't crash
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    // gets the exception
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    // logs the exception
                    var factory = app.Services.GetRequiredService<ILoggerFactory>();
                    var logger = factory.CreateLogger("WebApplicationExtensions");
                    logger.LogError(exception, "An unhandled exception occurred.");

                    // returns an appropriate error message to the client
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    // a detailed message for dev and a sanitized one for production
                    var env = app.Services.GetRequiredService<IHostEnvironment>();
                    if (env.IsDevelopment())
                    {
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = exception?.Message,
                            stackTrace = exception?.StackTrace
                        });
                    }
                    else
                    {
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "An unexpected error occurred. Please try again later."
                        });
                    }
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
            app.UseCancelLogic();
            return app;
        }

        public static WebApplication UseCancelLogic(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (OperationCanceledException)
                {
                    if (!context.RequestAborted.IsCancellationRequested)
                        throw; // real problem, rethrow

                    context.Response.StatusCode = 499; // Client Closed Request
                }
            });

            return app;
        }
    }
}