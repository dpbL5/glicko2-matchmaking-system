using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QueueProcessService.Application;

public sealed class MatchmakingWorker : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MatchmakingWorker> logger;
    
    // Configurable parameters (could be in appsettings.json)
    private const int IntervalMs = 5000;
    private const int MinPlayers = 2;
    private const decimal MaxSrDelta = 50m;

    public MatchmakingWorker(IServiceProvider serviceProvider, ILogger<MatchmakingWorker> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Matchmaking Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMatchmakingPass(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during matchmaking pass.");
            }

            await Task.Delay(IntervalMs, stoppingToken);
        }

        logger.LogInformation("Matchmaking Worker stopped.");
    }

    private async Task RunMatchmakingPass(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
        var upstreamClient = scope.ServiceProvider.GetRequiredService<IQueueUpstreamClient>();

        var candidateGroups = await repository.FindMatchCandidateGroupsAsync(MinPlayers, MaxSrDelta, ct);
        if (candidateGroups.Count == 0)
        {
            return;
        }

        int startedWorkflows = 0;

        foreach (var group in candidateGroups)
        {
            try
            {
                var initialized = await upstreamClient.InitializeMatchmakingAsync(group, queueId: null, ct);
                if (initialized)
                {
                    startedWorkflows++;
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Failed to initialize matchmaking workflow for group {Group}", string.Join(", ", group));
            }
        }

        if (startedWorkflows > 0)
        {
            logger.LogInformation("Matchmaking pass completed. Workflows started: {Count}", startedWorkflows);
        }
    }
}
