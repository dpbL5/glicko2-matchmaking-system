using System.Net;
using System.Net.Http.Json;
using QueueProcessService.Application;

namespace QueueProcessService.Infrastructure;

public sealed class QueueUpstreamClient : IQueueUpstreamClient
{
    private readonly IHttpClientFactory httpClientFactory;

    public QueueUpstreamClient(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<bool> PlayerExistsAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("PlayerService");
        using var response = await client.GetAsync($"/players/{playerId}", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        throw new HttpRequestException($"Player service returned {(int)response.StatusCode} ({response.ReasonPhrase}).", null, response.StatusCode);
    }

    public async Task<decimal?> GetCurrentRatingAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("RatingService");

        using var response = await client.GetAsync($"/ratings/{playerId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Rating service returned {(int)response.StatusCode} ({response.ReasonPhrase}).", null, response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<RatingResponseDto>(cancellationToken: cancellationToken);
        return payload?.Rating;
    }

    public async Task<MatchInitializationResult?> InitializeMatchmakingAsync(IReadOnlyDictionary<Guid, decimal> playerRatings, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("MatchmakingProcessService");
        using var response = await client.PostAsJsonAsync("/mm", new MatchInitRequestDto
        {
            PlayerRatings = playerRatings.ToDictionary(entry => entry.Key, entry => entry.Value),
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<MatchInitResultDto>(cancellationToken: cancellationToken);
            if (payload is null || payload.MatchId == Guid.Empty)
            {
                return null;
            }

            return new MatchInitializationResult
            {
                MatchId = payload.MatchId,
                PlayerIds = payload.PlayerIds,
                DequeuedPlayerIds = payload.DequeuedPlayerIds
            };
        }

        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        throw new HttpRequestException($"Matchmaking process service returned {(int)response.StatusCode} ({response.ReasonPhrase}).", null, response.StatusCode);
    }

    private sealed class RatingResponseDto
    {
        public decimal Rating { get; init; }
    }

    private sealed class MatchInitRequestDto
    {
        public Dictionary<Guid, decimal> PlayerRatings { get; init; } = [];
    }

    private sealed class MatchInitResultDto
    {
        public Guid MatchId { get; init; }

        public IReadOnlyList<Guid> PlayerIds { get; init; } = [];

        public IReadOnlyList<Guid> DequeuedPlayerIds { get; init; } = [];
    }

}