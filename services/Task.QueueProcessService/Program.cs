using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QueueProcessService.Application;
using QueueProcessService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .AddOptions<QueueDatabaseOptions>()
    .BindConfiguration(QueueDatabaseOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "QueueDatabase:Host is required.")
    .Validate(options => options.Port > 0 && options.Port <= 65535, "QueueDatabase:Port must be between 1 and 65535.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Name), "QueueDatabase:Name is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.User), "QueueDatabase:User is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "QueueDatabase:Password is required.")
    .ValidateOnStart();

builder.Services.AddHttpClient("PlayerService", client =>
{
    var baseUrl = builder.Configuration["PlayerService:BaseUrl"]
        ?? Environment.GetEnvironmentVariable("PLAYER_SERVICE_BASE_URL")
        ?? "http://player-service:5001";

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddHttpClient("RatingService", client =>
{
    var baseUrl = builder.Configuration["RatingService:BaseUrl"]
        ?? Environment.GetEnvironmentVariable("RATING_SERVICE_BASE_URL")
        ?? "http://rating-service:5005";

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddDbContext<QueueDbContext>((serviceProvider, options) =>
{
    var dbOptions = serviceProvider.GetRequiredService<IOptions<QueueDatabaseOptions>>().Value;
    var connectionString = $"Server={dbOptions.Host};Port={dbOptions.Port};Database={dbOptions.Name};User={dbOptions.User};Password={dbOptions.Password};SslMode=None;AllowPublicKeyRetrieval=True;";
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

    options.UseMySql(connectionString, serverVersion);
});

builder.Services.AddScoped<QueueDatabaseInitializer>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.AddScoped<IQueueUpstreamClient, QueueUpstreamClient>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        app.Logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = app.Environment.IsDevelopment() ? exception?.Message : null,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<QueueDatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

app.MapControllers();

app.Run();