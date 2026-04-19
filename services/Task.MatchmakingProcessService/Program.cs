using MatchmakingProcessService.Application;
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
