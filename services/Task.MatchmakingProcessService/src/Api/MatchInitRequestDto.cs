namespace MatchmakingProcessService.Api;

public sealed class MatchInitRequestDto
{
    public Dictionary<Guid, decimal> PlayerRatings { get; init; } = [];
}
