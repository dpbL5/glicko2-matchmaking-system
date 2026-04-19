using Entity.MatchService.Application;
using Entity.MatchService.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var matchDatabaseOptions = ResolveMatchDatabaseOptions(builder.Configuration);
ValidateMatchDatabaseOptions(matchDatabaseOptions);

builder.Services.AddSingleton(matchDatabaseOptions);

builder.Services.AddDbContext<MatchDbContext>((serviceProvider, options) =>
{
    var dbOptions = serviceProvider.GetRequiredService<MatchDatabaseOptions>();
    var connectionString = $"Server={dbOptions.Host};Port={dbOptions.Port};Database={dbOptions.Name};User={dbOptions.User};Password={dbOptions.Password};SslMode=None;AllowPublicKeyRetrieval=True;";

    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddScoped<MatchDatabaseInitializer>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((_, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
            ?? "rabbitmq";

        var port = ResolveIntConfiguration(builder.Configuration["RabbitMq:Port"], Environment.GetEnvironmentVariable("RABBITMQ_PORT"), 5672);

        var virtualHost = builder.Configuration["RabbitMq:VirtualHost"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_VHOST")
            ?? "/";

        var username = builder.Configuration["RabbitMq:Username"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_USER")
            ?? "guest";

        var password = builder.Configuration["RabbitMq:Password"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
            ?? "guest";

        cfg.Host(host, (ushort)port, virtualHost, h =>
        {
            h.Username(username);
            h.Password(password);
        });
    });
});

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
    var initializer = scope.ServiceProvider.GetRequiredService<MatchDatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

app.MapControllers();

app.Run();

static MatchDatabaseOptions ResolveMatchDatabaseOptions(IConfiguration configuration)
{
    var sectionOptions = configuration.GetSection(MatchDatabaseOptions.SectionName).Get<MatchDatabaseOptions>()
        ?? new MatchDatabaseOptions();

    var host = Environment.GetEnvironmentVariable("DB_HOST")
        ?? Environment.GetEnvironmentVariable("MATCH_DB_HOST")
        ?? sectionOptions.Host;

    var name = Environment.GetEnvironmentVariable("DB_NAME")
        ?? Environment.GetEnvironmentVariable("MATCH_DB_NAME")
        ?? sectionOptions.Name;

    var user = Environment.GetEnvironmentVariable("DB_USER")
        ?? Environment.GetEnvironmentVariable("MATCH_DB_USER")
        ?? sectionOptions.User;

    var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
        ?? Environment.GetEnvironmentVariable("MATCH_DB_PASSWORD")
        ?? sectionOptions.Password;

    var port = sectionOptions.Port;
    var configuredPort = Environment.GetEnvironmentVariable("DB_PORT")
        ?? Environment.GetEnvironmentVariable("MATCH_DB_INTERNAL_PORT")
        ?? Environment.GetEnvironmentVariable("MATCH_DB_PORT");

    if (!string.IsNullOrWhiteSpace(configuredPort) && int.TryParse(configuredPort, out var parsedPort))
    {
        port = parsedPort;
    }

    return new MatchDatabaseOptions
    {
        Host = host,
        Port = port,
        Name = name,
        User = user,
        Password = password
    };
}

static void ValidateMatchDatabaseOptions(MatchDatabaseOptions options)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(options.Host))
    {
        errors.Add("DB_HOST (or MATCH_DB_HOST / MatchDatabase:Host) is required.");
    }

    if (options.Port <= 0 || options.Port > 65535)
    {
        errors.Add("DB_PORT must be between 1 and 65535.");
    }

    if (string.IsNullOrWhiteSpace(options.Name))
    {
        errors.Add("DB_NAME (or MATCH_DB_NAME / MatchDatabase:Name) is required.");
    }

    if (string.IsNullOrWhiteSpace(options.User))
    {
        errors.Add("DB_USER (or MATCH_DB_USER / MatchDatabase:User) is required.");
    }

    if (string.IsNullOrWhiteSpace(options.Password))
    {
        errors.Add("DB_PASSWORD (or MATCH_DB_PASSWORD / MatchDatabase:Password) is required.");
    }

    if (errors.Count > 0)
    {
        throw new InvalidOperationException($"Invalid database configuration: {string.Join(" ", errors)}");
    }
}

static int ResolveIntConfiguration(string? configuredValue, string? envValue, int fallback)
{
    if (!string.IsNullOrWhiteSpace(envValue) && int.TryParse(envValue, out var parsedEnvValue))
    {
        return parsedEnvValue;
    }

    if (!string.IsNullOrWhiteSpace(configuredValue) && int.TryParse(configuredValue, out var parsedConfiguredValue))
    {
        return parsedConfiguredValue;
    }

    return fallback;
}