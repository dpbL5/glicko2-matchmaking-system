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
        using var response = await client.GetAsync($"/player/{playerId}", cancellationToken);

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
        using var response = await client.GetAsync($"/rating/{playerId}", cancellationToken);

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

    private sealed class RatingResponseDto
    {
        public decimal Rating { get; init; }
    }
}