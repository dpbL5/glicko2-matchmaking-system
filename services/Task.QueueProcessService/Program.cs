using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueProcessService.Application;
using QueueProcessService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var queueDatabaseOptions = ResolveQueueDatabaseOptions(builder.Configuration);
ValidateQueueDatabaseOptions(queueDatabaseOptions);

builder.Services.AddSingleton(queueDatabaseOptions);

builder.Services.AddHttpClient("PlayerService", client =>
{
    var baseUrl = ResolveBaseUrl(
        builder.Configuration["PlayerService:BaseUrl"],
        Environment.GetEnvironmentVariable("PLAYER_SERVICE_BASE_URL"),
        "http://player-service:5001");

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddHttpClient("RatingService", client =>
{
    var baseUrl = ResolveBaseUrl(
        builder.Configuration["RatingService:BaseUrl"],
        Environment.GetEnvironmentVariable("RATING_SERVICE_BASE_URL"),
        "http://rating-service:5005");

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddHttpClient("MatchmakingProcessService", client =>
{
    var baseUrl = ResolveBaseUrl(
        builder.Configuration["MatchmakingProcessService:BaseUrl"],
        Environment.GetEnvironmentVariable("MATCHMAKING_PROCESS_SERVICE_BASE_URL"),
        "http://matchmaking-process-service:5004");

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddDbContext<QueueDbContext>((serviceProvider, options) =>
{
    var dbOptions = serviceProvider.GetRequiredService<QueueDatabaseOptions>();
    var connectionString = $"Server={dbOptions.Host};Port={dbOptions.Port};Database={dbOptions.Name};User={dbOptions.User};Password={dbOptions.Password};SslMode=None;AllowPublicKeyRetrieval=True;";

    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
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

static QueueDatabaseOptions ResolveQueueDatabaseOptions(IConfiguration configuration)
{
    var sectionOptions = configuration.GetSection(QueueDatabaseOptions.SectionName).Get<QueueDatabaseOptions>()
        ?? new QueueDatabaseOptions();

    var host = Environment.GetEnvironmentVariable("DB_HOST")
        ?? Environment.GetEnvironmentVariable("QUEUE_DB_HOST")
        ?? sectionOptions.Host;

    var name = Environment.GetEnvironmentVariable("DB_NAME")
        ?? Environment.GetEnvironmentVariable("QUEUE_DB_NAME")
        ?? sectionOptions.Name;

    var user = Environment.GetEnvironmentVariable("DB_USER")
        ?? Environment.GetEnvironmentVariable("QUEUE_DB_USER")
        ?? sectionOptions.User;

    var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
        ?? Environment.GetEnvironmentVariable("QUEUE_DB_PASSWORD")
        ?? sectionOptions.Password;

    var port = sectionOptions.Port;
    var configuredPort = Environment.GetEnvironmentVariable("DB_PORT")
        ?? Environment.GetEnvironmentVariable("QUEUE_DB_INTERNAL_PORT")
        ?? Environment.GetEnvironmentVariable("QUEUE_DB_PORT");

    if (!string.IsNullOrWhiteSpace(configuredPort) && int.TryParse(configuredPort, out var parsedPort))
    {
        port = parsedPort;
    }

    return new QueueDatabaseOptions
    {
        Host = host,
        Port = port,
        Name = name,
        User = user,
        Password = password
    };
}

static void ValidateQueueDatabaseOptions(QueueDatabaseOptions options)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(options.Host))
    {
        errors.Add("DB_HOST (or QUEUE_DB_HOST / QueueDatabase:Host) is required.");
    }

    if (options.Port <= 0 || options.Port > 65535)
    {
        errors.Add("DB_PORT must be between 1 and 65535.");
    }

    if (string.IsNullOrWhiteSpace(options.Name))
    {
        errors.Add("DB_NAME (or QUEUE_DB_NAME / QueueDatabase:Name) is required.");
    }

    if (string.IsNullOrWhiteSpace(options.User))
    {
        errors.Add("DB_USER (or QUEUE_DB_USER / QueueDatabase:User) is required.");
    }

    if (string.IsNullOrWhiteSpace(options.Password))
    {
        errors.Add("DB_PASSWORD (or QUEUE_DB_PASSWORD / QueueDatabase:Password) is required.");
    }

    if (errors.Count > 0)
    {
        throw new InvalidOperationException($"Invalid database configuration: {string.Join(" ", errors)}");
    }
}

static string ResolveBaseUrl(params string?[] candidates)
{
    foreach (var candidate in candidates)
    {
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return candidate;
        }
    }

    return string.Empty;
}