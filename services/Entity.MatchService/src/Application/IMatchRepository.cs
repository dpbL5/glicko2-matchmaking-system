using Entity.MatchService.Domain;

namespace Entity.MatchService.Application;

public interface IMatchRepository
{
    Task<Match> CreateAsync(Match match, CancellationToken cancellationToken);

    Task<Match?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Match?> UpdateResultAsync(Guid id, Guid winner, string result, CancellationToken cancellationToken);

    Task<Match?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken);
}