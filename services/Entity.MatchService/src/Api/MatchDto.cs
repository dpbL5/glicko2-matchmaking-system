namespace Entity.MatchService.Api;

public sealed class MatchDto
{
    public Guid Id { get; init; }

    public string Status { get; init; } = Entity.MatchService.Domain.MatchStatus.Pending;

    public IReadOnlyList<Guid> PlayerIds { get; init; } = [];
}