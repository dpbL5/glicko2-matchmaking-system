using MatchmakingProcessService.Application;
using MatchmakingProcessService.Infrastructure.Messaging;
using MassTransit;
using MatchmakingProcessService.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHttpClient("MatchService", client =>
{
    var baseUrl = builder.Configuration["MatchService:BaseUrl"]
        ?? Environment.GetEnvironmentVariable("MATCH_SERVICE_BASE_URL")
        ?? "http://match-service:5002";

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddHttpClient("QueueService", client =>
{
    var baseUrl = builder.Configuration["QueueService:BaseUrl"]
        ?? Environment.GetEnvironmentVariable("QUEUE_SERVICE_BASE_URL")
        ?? "http://queue-service:5003";

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<RatingUpdatedConsumer>();
    x.AddConsumer<MatchUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
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

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<IMatchmakingOrchestrator, MatchmakingOrchestrator>();

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

app.MapControllers();

app.Run();

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
