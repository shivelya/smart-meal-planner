using Backend.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();

builder.Services.AddDatabase(builder.Configuration)
    .AddJwtAuth(builder.Configuration)
    .ConfigureSwagger()
    .AddEndpointsApiExplorer()
    .AddAppServices()
    .AddControllers();

var app = builder.Build();

app.UseMyAppPipeline()
    .ConfigureExceptionHandling()
    .MapControllers();
    
app.Run();