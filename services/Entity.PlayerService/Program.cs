using Entity.PlayerService.Infrastructure;
using Entity.PlayerService.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var playerDatabaseOptions = ResolvePlayerDatabaseOptions(builder.Configuration);
ValidatePlayerDatabaseOptions(playerDatabaseOptions);

builder.Services.AddSingleton(playerDatabaseOptions);

builder.Services.AddDbContext<PlayerDbContext>((serviceProvider, options) =>
{
    var dbOptions = serviceProvider.GetRequiredService<PlayerDatabaseOptions>();
    var connectionString = $"Server={dbOptions.Host};Port={dbOptions.Port};Database={dbOptions.Name};User={dbOptions.User};Password={dbOptions.Password};SslMode=None;AllowPublicKeyRetrieval=True;";

    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddScoped<PlayerDatabaseInitializer>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();

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
    var initializer = scope.ServiceProvider.GetRequiredService<PlayerDatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

app.MapControllers();

app.Run();

static PlayerDatabaseOptions ResolvePlayerDatabaseOptions(IConfiguration configuration)
{
    var sectionOptions = configuration.GetSection(PlayerDatabaseOptions.SectionName).Get<PlayerDatabaseOptions>()
        ?? new PlayerDatabaseOptions();

    var host = Environment.GetEnvironmentVariable("DB_HOST")
        ?? Environment.GetEnvironmentVariable("PLAYER_DB_HOST")
        ?? sectionOptions.Host;

    var name = Environment.GetEnvironmentVariable("DB_NAME")
        ?? Environment.GetEnvironmentVariable("PLAYER_DB_NAME")
        ?? sectionOptions.Name;

    var user = Environment.GetEnvironmentVariable("DB_USER")
        ?? Environment.GetEnvironmentVariable("PLAYER_DB_USER")
        ?? sectionOptions.User;

    var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
        ?? Environment.GetEnvironmentVariable("PLAYER_DB_PASSWORD")
        ?? sectionOptions.Password;

    var port = sectionOptions.Port;
    var configuredPort = Environment.GetEnvironmentVariable("DB_PORT")
        ?? Environment.GetEnvironmentVariable("PLAYER_DB_INTERNAL_PORT")
        ?? Environment.GetEnvironmentVariable("PLAYER_DB_PORT");

    if (!string.IsNullOrWhiteSpace(configuredPort) && int.TryParse(configuredPort, out var parsedPort))
    {
        port = parsedPort;
    }

    return new PlayerDatabaseOptions
    {
        Host = host,
        Port = port,
        Name = name,
        User = user,
        Password = password
    };
}

static void ValidatePlayerDatabaseOptions(PlayerDatabaseOptions options)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(options.Host))
    {
        errors.Add("DB_HOST (or PLAYER_DB_HOST / PlayerDatabase:Host) is required.");
    }

    if (options.Port <= 0 || options.Port > 65535)
    {
        errors.Add("DB_PORT must be between 1 and 65535.");
    }

    if (string.IsNullOrWhiteSpace(options.Name))
    {
        errors.Add("DB_NAME (or PLAYER_DB_NAME / PlayerDatabase:Name) is required.");
    }

    if (string.IsNullOrWhiteSpace(options.User))
    {
        errors.Add("DB_USER (or PLAYER_DB_USER / PlayerDatabase:User) is required.");
    }

    if (string.IsNullOrWhiteSpace(options.Password))
    {
        errors.Add("DB_PASSWORD (or PLAYER_DB_PASSWORD / PlayerDatabase:Password) is required.");
    }

    if (errors.Count > 0)
    {
        throw new InvalidOperationException($"Invalid database configuration: {string.Join(" ", errors)}");
    }
}