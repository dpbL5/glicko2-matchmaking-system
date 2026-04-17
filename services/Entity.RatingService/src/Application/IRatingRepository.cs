using Entity.RatingService.Domain;

namespace Entity.RatingService.Application;

public interface IRatingRepository
{
    Task<PlayerRating?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken);

    Task<PlayerRating> UpsertAsync(PlayerRating rating, CancellationToken cancellationToken);
}
