import { defineStore } from 'pinia';
import { playerService, queueService, matchService, ratingService, matchmakingProcessService } from '@/services';
import type { Player, QueueTicket, Match, PlayerRating } from '@/types';

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Unknown error';
}

export const useMatchmakingStore = defineStore('matchmaking', {
  state: () => ({
    // Players
    players: [] as Player[],
    selectedPlayer: null as Player | null,
    
    // Queue
    queueTicket: null as QueueTicket | null,
    queueStatus: 'idle' as 'idle' | 'searching' | 'matched' | 'submitting' | 'completed' | 'error',
    sseConnection: null as EventSource | null,
    
    // Match
    currentMatch: null as Match | null,
    
    // Ratings
    selectedPlayerRating: null as PlayerRating | null,
    opponentDetails: [] as Array<{ player: Player; rating: number | null }>,
    lastMatchOutcome: null as 'win' | 'draw' | 'loss' | null,
    
    // UI
    logs: [] as string[],
    serviceHealth: {
      players: false,
      queue: false,
      match: false,
      rating: false,
      matchmaking: false,
    },
  }),
  
  actions: {
    addLog(message: string) {
      this.logs.unshift(`[${new Date().toLocaleTimeString()}] ${message}`);
    },

    // Health checks
    async checkServiceHealth() {
      this.addLog('Checking service health...');
      try {
        this.serviceHealth.players = !!(await playerService.getHealth());
        this.addLog('✓ Player Service healthy');
      } catch {
        this.addLog('✗ Player Service unavailable');
      }

      try {
        this.serviceHealth.queue = !!(await queueService.getHealth());
        this.addLog('✓ Queue Service healthy');
      } catch {
        this.addLog('✗ Queue Service unavailable');
      }

      try {
        this.serviceHealth.match = !!(await matchService.getHealth());
        this.addLog('✓ Match Service healthy');
      } catch {
        this.addLog('✗ Match Service unavailable');
      }

      try {
        this.serviceHealth.rating = !!(await ratingService.getHealth());
        this.addLog('✓ Rating Service healthy');
      } catch {
        this.addLog('✗ Rating Service unavailable');
      }

      try {
        this.serviceHealth.matchmaking = !!(await matchmakingProcessService.getHealth());
        this.addLog('✓ Matchmaking Process Service healthy');
      } catch {
        this.addLog('✗ Matchmaking Process Service unavailable');
      }
    },

    // Player operations
    async fetchPlayers() {
      this.addLog('Fetching player list...');
      try {
        this.players = await playerService.listPlayers();
        this.addLog(`✓ Loaded ${this.players.length} players`);
      } catch (error: unknown) {
        this.addLog(`✗ Failed to fetch players: ${getErrorMessage(error)}`);
      }
    },

    async selectPlayer(playerId: string) {
      try {
        this.selectedPlayer = await playerService.getPlayer(playerId);
        this.addLog(`✓ Selected player: ${this.selectedPlayer.name}`);
        this.resetMatchFlow();
        await this.getPlayerRating(playerId);
      } catch (error: unknown) {
        this.addLog(`✗ Failed to select player: ${getErrorMessage(error)}`);
      }
    },

    async findMatch(playerId: string) {
      this.resetMatchFlow();
      this.queueStatus = 'searching';
      this.addLog(`Finding opponents for player ${playerId.substring(0, 8)}...`);

      try {
        this.queueTicket = await queueService.enqueue({ playerId });
        this.addLog(`✓ Queue ticket created for selected player`);
        this.startQueueStream(playerId);

        const opponents = await this.selectOpponents(playerId, 1);
        if (opponents.length === 0)
        {
          throw new Error('No opponents available for matchmaking.');
        }

        for (const opponent of opponents) {
          await queueService.enqueue({ playerId: opponent.id });
        }

        const result = await matchmakingProcessService.initializeMatchmaking({
          playerIds: [playerId, ...opponents.map((opponent) => opponent.id)],
          queueId: null,
        });

        this.currentMatch = {
          id: result.matchId,
          status: result.status,
          playerIds: result.playerIds,
        };

        this.opponentDetails = await this.getOpponentDetails(result.playerIds, playerId);
        this.queueStatus = 'matched';
        this.addLog(`✓ Match created: ${result.matchId.substring(0, 8)}`);
        this.addLog(`✓ Opponents found: ${this.opponentDetails.map((item) => item.player.name).join(', ')}`);

        if (this.sseConnection) {
          this.sseConnection.close();
          this.sseConnection = null;
        }
      } catch (error: unknown) {
        this.queueStatus = 'error';
        this.addLog(`✗ Matchmaking failed: ${getErrorMessage(error)}`);
      }
    },

    async leaveQueue(playerId: string) {
      try {
        await queueService.removeFromQueue(playerId);
        this.addLog('✓ Left queue');
        this.resetMatchFlow();
      } catch (error: unknown) {
        this.addLog(`✗ Failed to leave queue: ${getErrorMessage(error)}`);
      }
    },

    startQueueStream(playerId: string) {
      if (this.sseConnection) this.sseConnection.close();
      
      this.addLog('Opening real-time queue stream...');
      this.sseConnection = queueService.streamQueueUpdates(
        playerId,
        (data) => {
          const queueData = data as QueueTicket;
          this.addLog(`→ Queue Update: ${queueData.status}`);
          this.queueTicket = queueData;

          if (queueData.status === 'MATCHED') {
            this.addLog('✓ Queue marked as matched. Waiting for match details...');
          }
        },
        (_error) => {
          this.addLog('✗ Queue stream disconnected');
          if (this.sseConnection?.readyState !== EventSource.CLOSED) {
            this.sseConnection?.close();
          }
        }
      );
    },

    async submitMatchOutcome(outcome: 'win' | 'draw' | 'loss') {
      if (!this.currentMatch || !this.selectedPlayer) {
        this.addLog('✗ Cannot submit result without an active match and selected player.');
        return;
      }

      this.queueStatus = 'submitting';
      this.lastMatchOutcome = outcome;
      this.addLog(`Submitting match outcome: ${outcome.toUpperCase()}`);

      try {
        const primaryOpponentId = this.opponentDetails[0]?.player.id;
        const winner = outcome === 'win'
          ? this.selectedPlayer.id
          : outcome === 'loss'
            ? (primaryOpponentId ?? 'opponent')
            : 'draw';

        const updatedMatch = await matchService.submitMatchResult(this.currentMatch.id, {
          winner,
          result: outcome,
        });

        this.currentMatch.status = updatedMatch.status;

        const opponentRatings = this.opponentDetails
          .map((item) => item.rating)
          .filter((rating): rating is number => rating !== null);

        const recalculated = await ratingService.recalculatePlayerRating(this.selectedPlayer.id, {
          matchResult: outcome,
          opponentRatings,
        });

        this.selectedPlayerRating = recalculated;
        this.queueStatus = 'completed';
        this.addLog(`✓ Match result accepted. New rating: ${recalculated.rating.toFixed(2)}`);
      } catch (error: unknown) {
        this.queueStatus = 'error';
        this.addLog(`✗ Failed to submit match outcome: ${getErrorMessage(error)}`);
      }
    },

    // Rating operations
    async getPlayerRating(playerId: string) {
      try {
        this.selectedPlayerRating = await ratingService.getPlayerRating(playerId);
        this.addLog(`✓ Player rating: ${this.selectedPlayerRating.rating.toFixed(1)}`);
      } catch (error: unknown) {
        this.addLog(`✗ Failed to fetch rating: ${getErrorMessage(error)}`);
      }
    },

    async updatePlayerRating(playerId: string, rating: number, rd: number, volatility: number) {
      this.addLog(`Updating rating for ${playerId.substring(0, 8)}`);
      try {
        this.selectedPlayerRating = await ratingService.updatePlayerRating(playerId, {
          rating,
          rd,
          volatility,
        });
        this.addLog(`✓ Rating updated: ${this.selectedPlayerRating.rating.toFixed(1)}`);
      } catch (error: unknown) {
        this.addLog(`✗ Failed to update rating: ${getErrorMessage(error)}`);
      }
    },

    async selectOpponents(playerId: string, count: number): Promise<Player[]> {
      const candidates = this.players.filter((player) => player.id !== playerId);
      if (candidates.length === 0) {
        return [];
      }

      const selectedRating = this.selectedPlayerRating?.rating ?? 1500;
      const ratedCandidates = await Promise.all(
        candidates.map(async (candidate) => {
          try {
            const rating = await ratingService.getPlayerRating(candidate.id);
            return { player: candidate, rating: rating.rating };
          } catch {
            return { player: candidate, rating: null as number | null };
          }
        }),
      );

      ratedCandidates.sort((a, b) => {
        const left = a.rating === null ? Number.MAX_SAFE_INTEGER : Math.abs(a.rating - selectedRating);
        const right = b.rating === null ? Number.MAX_SAFE_INTEGER : Math.abs(b.rating - selectedRating);
        return left - right;
      });

      return ratedCandidates.slice(0, count).map((item) => item.player);
    },

    async getOpponentDetails(playerIds: string[], selectedPlayerId: string): Promise<Array<{ player: Player; rating: number | null }>> {
      const opponentIds = playerIds.filter((playerId) => playerId !== selectedPlayerId);

      return Promise.all(opponentIds.map(async (opponentId) => {
        const cached = this.players.find((player) => player.id === opponentId);
        const player = cached ?? await playerService.getPlayer(opponentId);

        try {
          const rating = await ratingService.getPlayerRating(opponentId);
          return { player, rating: rating.rating };
        } catch {
          return { player, rating: null };
        }
      }));
    },

    resetMatchFlow() {
      this.queueTicket = null;
      this.currentMatch = null;
      this.opponentDetails = [];
      this.lastMatchOutcome = null;
      this.queueStatus = 'idle';

      if (this.sseConnection) {
        this.sseConnection.close();
        this.sseConnection = null;
      }
    },

    // Cleanup
    cleanup() {
      this.resetMatchFlow();
    },
  },
});
