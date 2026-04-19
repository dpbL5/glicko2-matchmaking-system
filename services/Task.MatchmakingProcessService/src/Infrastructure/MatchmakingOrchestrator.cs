using System.Net;
using System.Net.Http.Json;
using MassTransit;
using MatchmakingProcessService.Application;
using MatchmakingProcessService.Application.Messaging;
using MatchmakingProcessService.Domain;

namespace MatchmakingProcessService.Infrastructure;

public sealed class MatchmakingOrchestrator : IMatchmakingOrchestrator
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IPublishEndpoint publishEndpoint;

    public MatchmakingOrchestrator(IHttpClientFactory httpClientFactory, IPublishEndpoint publishEndpoint)
    {
        this.httpClientFactory = httpClientFactory;
        this.publishEndpoint = publishEndpoint;
    }

    public async Task<MatchInitializationResult> InitializeAsync(MatchInitializationRequest request, CancellationToken cancellationToken)
    {
        var match = await CreateMatchAsync(request, cancellationToken);

        var failedToDequeuePlayerIds = await TryConfirmMatchedPlayersAsync(match.Id, request.PlayerIds, cancellationToken);
        var dequeuedPlayerIds = request.PlayerIds.Except(failedToDequeuePlayerIds).ToList();

        if (failedToDequeuePlayerIds.Count > 0)
        {
            throw new MatchmakingConflictException(
                $"Match {match.Id} created but some queue tickets could not be dequeued: {string.Join(", ", failedToDequeuePlayerIds)}");
        }

        await publishEndpoint.Publish(new MatchReadyEvent
        {
            MatchId = match.Id,
            PlayerIds = request.PlayerIds,
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken);

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

    private async Task<IReadOnlyList<Guid>> TryConfirmMatchedPlayersAsync(Guid matchId, IReadOnlyList<Guid> playerIds, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("QueueService");
        using var response = await client.PostAsJsonAsync("/queue/matched", new ConfirmQueueMatchedRequestDto
        {
            MatchId = matchId,
            PlayerIds = playerIds.ToList()
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await DequeueMatchedPlayersAsync(playerIds, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return playerIds;
        }

        throw new HttpRequestException(
            $"Queue service returned {(int)response.StatusCode} ({response.ReasonPhrase}) while confirming matched players.",
            null,
            response.StatusCode);
    }

    private sealed class ConfirmQueueMatchedRequestDto
    {
        public Guid MatchId { get; init; }

        public List<Guid> PlayerIds { get; init; } = [];
    }

    private async Task<IReadOnlyList<Guid>> DequeueMatchedPlayersAsync(IReadOnlyList<Guid> playerIds, CancellationToken cancellationToken)
    {
        var failedPlayerIds = new List<Guid>();

        foreach (var playerId in playerIds)
        {
            var client = httpClientFactory.CreateClient("QueueService");
            using var response = await client.DeleteAsync($"/queue/{playerId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                continue;
            }

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            {
                failedPlayerIds.Add(playerId);
                continue;
            }

            throw new HttpRequestException(
                $"Queue service returned {(int)response.StatusCode} ({response.ReasonPhrase}) while dequeuing player {playerId}.",
                null,
                response.StatusCode);
        }

        return failedPlayerIds;
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
