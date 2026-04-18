namespace Entity.MatchService.Api;

public sealed class MatchResultResponseDto
{
    public Guid MatchId { get; init; }

    public string Status { get; init; } = Entity.MatchService.Domain.MatchStatus.Finished;
}