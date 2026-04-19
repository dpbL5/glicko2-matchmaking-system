namespace MatchmakingProcessService.Application;

public interface IMatchmakingOrchestrator
{
    Task<MatchInitializationResult> InitializeAsync(MatchInitializationRequest request, CancellationToken cancellationToken);
}
