using System.Net;
using System.Net.Http.Json;
using MatchmakingProcessService.Application;
using MatchmakingProcessService.Domain;

namespace MatchmakingProcessService.Infrastructure;

public sealed class MatchmakingOrchestrator : IMatchmakingOrchestrator
{
    private readonly IHttpClientFactory httpClientFactory;

    public MatchmakingOrchestrator(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<MatchInitializationResult> InitializeAsync(MatchInitializationRequest request, CancellationToken cancellationToken)
    {
        var match = await CreateMatchAsync(request, cancellationToken);

        var dequeuedPlayerIds = new List<Guid>();
        var failedToDequeuePlayerIds = new List<Guid>();

        foreach (var playerId in request.PlayerIds)
        {
            var removed = await TryDequeuePlayerAsync(playerId, cancellationToken);
            if (removed)
            {
                dequeuedPlayerIds.Add(playerId);
            }
            else
            {
                failedToDequeuePlayerIds.Add(playerId);
            }
        }

        if (failedToDequeuePlayerIds.Count > 0)
        {
            throw new MatchmakingConflictException(
                $"Match {match.Id} created but some queue tickets could not be dequeued: {string.Join(", ", failedToDequeuePlayerIds)}");
        }

        return new MatchInitializationResult
        {
            MatchId = match.Id,
            Status = match.Status,
            PlayerIds = request.PlayerIds,
            DequeuedPlayerIds = dequeuedPlayerIds,
            FailedToDequeuePlayerIds = failedToDequeuePlayerIds
        };
    }

    private async Task<MatchResponseDto> CreateMatchAsync(MatchInitializationRequest request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("MatchService");

        using var response = await client.PostAsJsonAsync("/matches", new MatchCreateRequestDto
        {
            PlayerIds = request.PlayerIds.ToList(),
            QueueId = request.QueueId
        }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new MatchmakingConflictException("Match service rejected the match initialization payload.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Match service returned {(int)response.StatusCode} ({response.ReasonPhrase}).",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<MatchResponseDto>(cancellationToken: cancellationToken);
        if (payload is null || payload.Id == Guid.Empty)
        {
            throw new HttpRequestException("Match service returned an invalid match payload.");
        }

        return payload;
    }

    private async Task<bool> TryDequeuePlayerAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("QueueService");
        using var response = await client.DeleteAsync($"/queue/{playerId}", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        throw new HttpRequestException(
            $"Queue service returned {(int)response.StatusCode} ({response.ReasonPhrase}) while dequeuing player {playerId}.",
            null,
            response.StatusCode);
    }

    private sealed class MatchCreateRequestDto
    {
        public List<Guid> PlayerIds { get; init; } = [];

        public string? QueueId { get; init; }
    }

    private sealed class MatchResponseDto
    {
        public Guid Id { get; init; }

        public string Status { get; init; } = string.Empty;
    }
}
