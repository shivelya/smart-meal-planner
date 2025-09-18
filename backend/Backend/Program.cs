using Backend.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();

if (!builder.Environment.IsEnvironment("Integration"))
    builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddJwtAuth(builder.Configuration)
    .ConfigureSwagger(builder.Configuration)
    .AddEndpointsApiExplorer()
    .AddAppServices()
    .AddControllers();

var app = builder.Build();

app.UseMyAppPipeline()
    .ConfigureExceptionHandling()
    .MapControllers();

app.Run();

public partial class Program { }