<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, computed } from 'vue';
import { useMatchmakingStore } from '../stores/matchmaking';

const store = useMatchmakingStore();
const selectedPlayerId = ref('');

const canFindMatch = computed(() => {
  return Boolean(selectedPlayerId.value) && ['idle', 'completed', 'error'].includes(store.queueStatus);
});

const canLeaveQueue = computed(() => {
  return Boolean(selectedPlayerId.value) && store.queueStatus === 'searching';
});

const canSubmitResult = computed(() => {
  return store.queueStatus === 'matched' && !!store.currentMatch;
});

const ratingDisplay = computed(() => {
  if (!store.selectedPlayerRating) return 'N/A';
  return store.selectedPlayerRating.rating.toFixed(2);
});

const statusLabel = computed(() => {
  return store.queueStatus.replace(/(^\w|\s\w)/g, (char) => char.toUpperCase());
});

const healthyCount = computed(() => {
  const values = Object.values(store.serviceHealth);
  return values.filter(Boolean).length;
});

onMounted(() => {
  store.checkServiceHealth();
  store.fetchPlayers();
});

onBeforeUnmount(() => {
  store.cleanup();
});

const findMatch = () => {
  if (selectedPlayerId.value) {
    store.findMatch(selectedPlayerId.value);
  }
};

const leaveQueue = () => {
  if (selectedPlayerId.value) {
    store.leaveQueue(selectedPlayerId.value);
  }
};

const submitOutcome = (outcome: 'win' | 'draw' | 'loss') => {
  store.submitMatchOutcome(outcome);
};

const selectPlayer = (playerId: string) => {
  selectedPlayerId.value = playerId;
  store.selectPlayer(playerId);
};
</script>

<template>
  <div class="screen">
    <header class="hero">
      <div>
        <p class="eyebrow">Live Match Orchestrator</p>
        <h1>Glicko2 Arena Console</h1>
        <p class="subtitle">Select player -> find opponents -> submit win/draw/loss -> see updated rating.</p>
      </div>
      <div class="status-pill" :data-state="store.queueStatus">
        <span class="status-dot"></span>
        <span>{{ statusLabel }}</span>
      </div>
    </header>

    <section class="service-strip">
      <div class="service-summary">
        <strong>{{ healthyCount }}/5</strong>
        <span>Services Healthy</span>
      </div>
      <div class="service-chip" :class="{ healthy: store.serviceHealth.players }">
        <span class="dot"></span>Players
      </div>
      <div class="service-chip" :class="{ healthy: store.serviceHealth.queue }">
        <span class="dot"></span>Queue
      </div>
      <div class="service-chip" :class="{ healthy: store.serviceHealth.matchmaking }">
        <span class="dot"></span>Matchmaking
      </div>
      <div class="service-chip" :class="{ healthy: store.serviceHealth.match }">
        <span class="dot"></span>Match
      </div>
      <div class="service-chip" :class="{ healthy: store.serviceHealth.rating }">
        <span class="dot"></span>Rating
      </div>
    </section>

    <main class="layout">
      <section class="left-column">
        <article class="panel panel-player">
          <h2>Player Selection</h2>
          <label class="field-label" for="player-select">Choose active player</label>
          <select
            id="player-select"
            @change="e => selectPlayer((e.target as HTMLSelectElement).value)"
            :value="selectedPlayerId"
            :disabled="store.queueStatus === 'searching' || store.queueStatus === 'submitting'"
          >
            <option value="" disabled>Select one player</option>
            <option v-for="player in store.players" :key="player.id" :value="player.id">
              {{ player.name }}
            </option>
          </select>

          <div v-if="store.selectedPlayer" class="player-card">
            <div class="kv"><span>ID</span><strong>{{ store.selectedPlayer.id.substring(0, 12) }}...</strong></div>
            <div class="kv"><span>Name</span><strong>{{ store.selectedPlayer.name }}</strong></div>
            <div class="kv"><span>Current Rating</span><strong class="rating">{{ ratingDisplay }}</strong></div>
          </div>
        </article>

        <article class="panel panel-flow">
          <h2>Match Flow</h2>

          <div v-if="store.queueStatus === 'idle' || store.queueStatus === 'completed'" class="state-block">
            <p class="state-line">Ready to start matchmaking.</p>
            <p v-if="store.lastMatchOutcome && store.queueStatus === 'completed'" class="state-line success">
              Last result: {{ store.lastMatchOutcome.toUpperCase() }}
            </p>
            <button class="btn btn-accent" :disabled="!canFindMatch" @click="findMatch">Find Opponents</button>
          </div>

          <div v-else-if="store.queueStatus === 'searching'" class="state-block searching">
            <div class="pulse"></div>
            <p class="state-line">Queue + matchmaking process in progress...</p>
            <p v-if="store.queueTicket" class="muted">Ticket for {{ store.queueTicket.playerId.substring(0, 8) }}...</p>
            <button class="btn btn-muted" :disabled="!canLeaveQueue" @click="leaveQueue">Leave Queue</button>
          </div>

          <div v-else-if="store.queueStatus === 'matched'" class="state-block matched">
            <p class="state-line success">Match is ready.</p>
            <div v-if="store.currentMatch" class="match-meta">
              <div class="kv"><span>Match ID</span><strong>{{ store.currentMatch.id.substring(0, 12) }}...</strong></div>
              <div class="kv"><span>Players</span><strong>{{ store.currentMatch.playerIds.length }}</strong></div>
            </div>

            <div class="opponents" v-if="store.opponentDetails.length > 0">
              <h3>Opponents</h3>
              <div class="opponent" v-for="item in store.opponentDetails" :key="item.player.id">
                <span>{{ item.player.name }}</span>
                <strong>SR {{ item.rating === null ? 'N/A' : item.rating.toFixed(2) }}</strong>
              </div>
            </div>
          </div>

          <div v-else-if="store.queueStatus === 'submitting'" class="state-block searching">
            <div class="pulse"></div>
            <p class="state-line">Submitting result and recalculating rating...</p>
          </div>

          <div v-else-if="store.queueStatus === 'error'" class="state-block error">
            <p class="state-line">Flow failed. Check logs and retry.</p>
            <button class="btn btn-accent" :disabled="!canFindMatch" @click="findMatch">Retry</button>
          </div>
        </article>
      </section>

      <section class="right-column">
        <article class="panel panel-result">
          <h2>Submit Match Result</h2>
          <p class="muted">Use official outcomes only: win, draw, or loss.</p>

          <div class="result-grid">
            <button class="btn btn-win" :disabled="!canSubmitResult" @click="submitOutcome('win')">Win</button>
            <button class="btn btn-draw" :disabled="!canSubmitResult" @click="submitOutcome('draw')">Draw</button>
            <button class="btn btn-loss" :disabled="!canSubmitResult" @click="submitOutcome('loss')">Loss</button>
          </div>

          <div class="result-note" v-if="store.queueStatus === 'completed'">
            <strong>Rating Updated:</strong>
            <span>{{ ratingDisplay }}</span>
          </div>
        </article>

        <article class="panel panel-logs">
          <h2>Event Log</h2>
          <div class="logs">
            <div v-if="store.logs.length === 0" class="log-empty">No events yet.</div>
            <div v-for="(log, i) in store.logs.slice(0, 30)" :key="i" class="log-entry">
              {{ log }}
            </div>
          </div>
        </article>
      </section>
    </main>
  </div>
</template>

<style scoped>
.screen {
  min-height: 100vh;
  padding: clamp(1rem, 2vw, 2rem);
  color: #f6f2e9;
}

.hero {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  padding: 1rem;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 18px;
  background: linear-gradient(120deg, rgba(12, 38, 56, 0.9), rgba(111, 57, 22, 0.7));
}

.eyebrow {
  font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  font-size: 0.72rem;
  color: #c7d6e3;
}

h1 {
  margin: 0.3rem 0;
  font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
  font-size: clamp(1.7rem, 3.4vw, 2.6rem);
  line-height: 1.1;
}

.subtitle {
  color: #d9dfd2;
  max-width: 46rem;
}

.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  border-radius: 999px;
  padding: 0.55rem 0.9rem;
  border: 1px solid rgba(255, 255, 255, 0.25);
  background: rgba(17, 22, 26, 0.55);
  font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
  font-size: 0.9rem;
}

.status-dot {
  width: 0.52rem;
  height: 0.52rem;
  border-radius: 50%;
  background: #f7c948;
}

.status-pill[data-state='matched'] .status-dot,
.status-pill[data-state='completed'] .status-dot {
  background: #55d6a7;
}

.status-pill[data-state='error'] .status-dot {
  background: #f97068;
}

.service-strip {
  margin-top: 0.9rem;
  display: flex;
  flex-wrap: wrap;
  gap: 0.55rem;
}

.service-summary,
.service-chip {
  border-radius: 10px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(20, 24, 28, 0.75);
  padding: 0.4rem 0.75rem;
  font-size: 0.86rem;
}

.service-summary {
  display: inline-flex;
  gap: 0.45rem;
  align-items: baseline;
}

.service-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  color: #b3b9bf;
}

.service-chip .dot {
  width: 0.46rem;
  height: 0.46rem;
  border-radius: 50%;
  background: #6c757d;
}

.service-chip.healthy {
  color: #dcffe8;
}

.service-chip.healthy .dot {
  background: #55d6a7;
}

.layout {
  margin-top: 1rem;
  display: grid;
  grid-template-columns: minmax(300px, 1fr) minmax(350px, 1.2fr);
  gap: 1rem;
}

.left-column,
.right-column {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.panel {
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(19, 23, 27, 0.78);
  padding: 1rem;
}

.panel h2 {
  margin: 0 0 0.75rem;
  font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
  font-size: 1.1rem;
}

.field-label {
  display: block;
  margin-bottom: 0.35rem;
  color: #c2c8ce;
  font-size: 0.88rem;
}

select {
  width: 100%;
  border-radius: 10px;
  padding: 0.7rem;
  color: #f0f3f6;
  background: rgba(11, 15, 19, 0.9);
  border: 1px solid rgba(255, 255, 255, 0.18);
}

select:disabled {
  opacity: 0.5;
}

.player-card,
.match-meta,
.result-note {
  margin-top: 0.8rem;
  border-radius: 12px;
  background: rgba(8, 11, 14, 0.72);
  border: 1px solid rgba(255, 255, 255, 0.08);
  padding: 0.8rem;
}

.kv {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.4rem 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.kv:last-child {
  border-bottom: none;
}

.kv span {
  color: #96a0aa;
}

.kv strong {
  font-family: 'JetBrains Mono', 'Consolas', monospace;
}

.kv .rating {
  color: #f7c948;
}

.state-block {
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  background: rgba(10, 13, 17, 0.78);
  padding: 0.85rem;
  display: grid;
  gap: 0.65rem;
}

.state-block.searching {
  border-color: rgba(247, 201, 72, 0.55);
}

.state-block.matched {
  border-color: rgba(85, 214, 167, 0.55);
}

.state-block.error {
  border-color: rgba(249, 112, 104, 0.55);
}

.state-line {
  margin: 0;
  font-weight: 500;
}

.state-line.success {
  color: #9df5d1;
}

.muted {
  margin: 0;
  color: #94a0ab;
  font-size: 0.9rem;
}

.opponents {
  margin-top: 0.65rem;
}

.opponents h3 {
  margin: 0 0 0.45rem;
  font-size: 0.94rem;
  color: #9cc6db;
}

.opponent {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.45rem 0;
  border-bottom: 1px dashed rgba(255, 255, 255, 0.12);
}

.opponent:last-child {
  border-bottom: none;
}

.result-grid {
  margin-top: 0.6rem;
  display: grid;
  gap: 0.55rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.btn {
  border: none;
  border-radius: 10px;
  padding: 0.7rem 0.9rem;
  color: #f9f5ed;
  font-family: 'Space Grotesk', 'Segoe UI', sans-serif;
  font-weight: 600;
  cursor: pointer;
}

.btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.btn-accent {
  background: linear-gradient(135deg, #f15b2a, #f7c948);
  color: #111;
}

.btn-muted {
  background: #5f6b77;
}

.btn-win {
  background: #198754;
}

.btn-draw {
  background: #b08900;
}

.btn-loss {
  background: #b02a37;
}

.logs {
  max-height: 360px;
  overflow: auto;
  display: grid;
  gap: 0.35rem;
}

.log-entry {
  font-family: 'JetBrains Mono', 'Consolas', monospace;
  font-size: 0.8rem;
  padding: 0.5rem;
  border-radius: 8px;
  background: rgba(9, 12, 15, 0.75);
  border: 1px solid rgba(255, 255, 255, 0.08);
}

.log-empty {
  color: #98a4ae;
}

.pulse {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 50%;
  background: #f7c948;
  box-shadow: 0 0 0 0 rgba(247, 201, 72, 0.8);
  animation: pulse 1.6s infinite;
}

@keyframes pulse {
  0% { box-shadow: 0 0 0 0 rgba(247, 201, 72, 0.8); }
  70% { box-shadow: 0 0 0 12px rgba(247, 201, 72, 0); }
  100% { box-shadow: 0 0 0 0 rgba(247, 201, 72, 0); }
}

@media (max-width: 980px) {
  .layout {
    grid-template-columns: 1fr;
  }

  .result-grid {
    grid-template-columns: 1fr;
  }

  .hero {
    flex-direction: column;
  }
}
</style>
