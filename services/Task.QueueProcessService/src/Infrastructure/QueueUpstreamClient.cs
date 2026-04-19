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

    public async Task<bool> InitializeMatchmakingAsync(IReadOnlyList<Guid> playerIds, string? queueId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("MatchmakingProcessService");
        using var response = await client.PostAsJsonAsync("/mm", new MatchInitRequestDto
        {
            PlayerIds = playerIds.ToList(),
            QueueId = queueId
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        throw new HttpRequestException($"Matchmaking process service returned {(int)response.StatusCode} ({response.ReasonPhrase}).", null, response.StatusCode);
    }

    private sealed class RatingResponseDto
    {
        public decimal Rating { get; init; }
    }

    private sealed class MatchInitRequestDto
    {
        public List<Guid> PlayerIds { get; init; } = [];

        public string? QueueId { get; init; }
    }
}