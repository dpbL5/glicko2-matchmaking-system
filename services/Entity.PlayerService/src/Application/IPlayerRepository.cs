namespace Entity.PlayerService.Application;

using Entity.PlayerService.Domain;

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken);

    Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}