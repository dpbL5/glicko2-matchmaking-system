namespace MatchmakingProcessService.Domain;

public sealed class MatchmakingConflictException : Exception
{
    public MatchmakingConflictException(string message)
        : base(message)
    {
    }
}
