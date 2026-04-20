<script setup>
import { ref, reactive, onMounted, computed, onUnmounted } from 'vue'
import {
  fetchPlayers,
  fetchRating,
  enqueuePlayer,
  subscribeQueueStream,
  dequeuePlayer,
  fetchMatch,
  submitMatchResult,
  fetchQueuedPlayers,
} from './api.js'

// ─── State ────────────────────────────────────────────
const players = ref([])
const playerRatings = reactive({})
const logs = ref([])
// Use array instead of Set for Vue reactivity
const queuedPlayerIds = ref([])

// Flow
const selectedPlayerId = ref(null)
const flowState = ref('idle')
const currentStream = ref(null)
const currentMatchId = ref(null)
const currentMatch = ref(null)
const matchPlayers = ref([])
const selectedWinner = ref(null)
const secondPlayerSelect = ref(null)
const queuePollTimer = ref(null)

// ─── Helpers ──────────────────────────────────────────
function addLog(tag, message, type = 'request') {
  const time = new Date().toLocaleTimeString('en-US', {
    hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit', fractionalSecondDigits: 1
  })
  logs.value.unshift({ time, tag, message, type, id: Date.now() + Math.random() })
  if (logs.value.length > 300) logs.value.length = 300
}

function shortId(id) {
  return id ? id.substring(0, 8) : '—'
}

function getPlayerName(id) {
  const p = players.value.find(pl => pl.id === id)
  return p ? p.name : shortId(id)
}

function isQueued(playerId) {
  return queuedPlayerIds.value.includes(playerId)
}

function addToQueued(playerId) {
  if (!queuedPlayerIds.value.includes(playerId)) {
    queuedPlayerIds.value = [...queuedPlayerIds.value, playerId]
  }
}

function removeFromQueued(playerId) {
  queuedPlayerIds.value = queuedPlayerIds.value.filter(id => id !== playerId)
}

const selectedPlayer = computed(() =>
  players.value.find(p => p.id === selectedPlayerId.value)
)

const availableSecondPlayers = computed(() =>
  players.value.filter(p =>
    p.id !== selectedPlayerId.value && !isQueued(p.id)
  )
)

// ─── Load Players & Ratings ──────────────────────────
async function loadPlayers() {
  try {
    addLog('REQ', 'GET /players', 'request')
    const data = await fetchPlayers()
    players.value = data
    addLog('RES', `200 OK — ${data.length} players loaded`, 'response')

    let loadedCount = 0
    for (const p of data) {
      try {
        const r = await fetchRating(p.id)
        if (r) {
          playerRatings[p.id] = r
          loadedCount++
        }
      } catch (_) { /* skip */ }
    }
    addLog('RES', `Loaded ${loadedCount} player ratings`, 'response')
  } catch (e) {
    addLog('ERR', `Failed to load players: ${e.message}`, 'error')
  }
}

// ─── Poll queued players (so we can show status) ─────
async function pollQueuedPlayers() {
  try {
    const queued = await fetchQueuedPlayers()
    queuedPlayerIds.value = queued.map(t => t.playerId)
  } catch (_) { /* ignore */ }
}

function startQueuePolling() {
  pollQueuedPlayers()
  queuePollTimer.value = setInterval(pollQueuedPlayers, 2000)
}

function stopQueuePolling() {
  if (queuePollTimer.value) {
    clearInterval(queuePollTimer.value)
    queuePollTimer.value = null
  }
}

// ─── Client Flow: Enqueue + SSE ──────────────────────
async function startMatchmaking() {
  if (!selectedPlayerId.value) return
  const pid = selectedPlayerId.value
  flowState.value = 'queuing'

  addLog('REQ', `POST /queue { playerId: "${shortId(pid)}" }`, 'request')
  try {
    const ticket = await enqueuePlayer(pid)
    addLog('RES', `201 Created — SR snapshot: ${ticket.sr}`, 'response')
    addToQueued(pid)
    flowState.value = 'searching'

    // Open SSE stream
    addLog('REQ', `GET /queue/${shortId(pid)}/stream (SSE)`, 'request')
    currentStream.value = subscribeQueueStream(
      pid,
      (ev) => handleStreamEvent(pid, ev),
      () => {
        addLog('ERR', 'SSE connection error/closed', 'error')
        if (flowState.value === 'searching') flowState.value = 'idle'
      }
    )

    // Start polling queue to show other queued players
    startQueuePolling()
  } catch (e) {
    addLog('ERR', e.message, 'error')
    flowState.value = 'idle'
  }
}

function handleStreamEvent(playerId, event) {
  if (event.type === 'stream-open') {
    addLog('SSE', `Stream opened for ${shortId(playerId)}`, 'event')
    return
  }
  if (event.type === 'stream-closed') {
    addLog('SSE', 'Stream closed by server', 'event')
    return
  }
  if (event.type !== 'queue-signal' || !event.data) return

  const { signal } = event.data

  if (signal === 'QUEUED') {
    addLog('SSE', `QUEUED — SR: ${event.data.sr}, waiting for opponent…`, 'event')
  } else if (signal === 'MATCH_FOUND') {
    stopQueuePolling()
    currentMatchId.value = event.data.matchId
    matchPlayers.value = event.data.playerIds || []
    removeFromQueued(playerId)
    // Also remove opponent from queued display
    for (const pid of matchPlayers.value) removeFromQueued(pid)

    addLog('SSE', `🎯 MATCH_FOUND!`, 'event')
    addLog('SSE', `  matchId: ${shortId(event.data.matchId)}`, 'event')
    addLog('SSE', `  ${matchPlayers.value.map(id => getPlayerName(id)).join(' vs ')}`, 'event')
    addLog('SSE', `  Flow: Queue → POST /mm → MatchService POST /matches → DELETE /queue → MatchReady`, 'event')

    flowState.value = 'matched'
    loadMatchDetails(event.data.matchId)
  } else if (signal === 'DEQUEUED') {
    removeFromQueued(playerId)
    addLog('SSE', 'DEQUEUED — removed from queue', 'event')
    flowState.value = 'idle'
    stopQueuePolling()
  }
}

async function loadMatchDetails(matchId) {
  try {
    addLog('REQ', `GET /matches/${shortId(matchId)}`, 'request')
    const match = await fetchMatch(matchId)
    if (match) {
      currentMatch.value = match
      addLog('RES', `200 OK — status: ${match.status}, players: ${match.playerIds?.length ?? '?'}`, 'response')
      flowState.value = 'playing'
    }
  } catch (e) {
    addLog('ERR', e.message, 'error')
  }
}

// ─── Enqueue 2nd Player ──────────────────────────────
async function enqueueSecondPlayer() {
  if (!secondPlayerSelect.value) return
  const pid = secondPlayerSelect.value

  addLog('REQ', `POST /queue { playerId: "${shortId(pid)}" } (2nd player)`, 'request')
  try {
    const ticket = await enqueuePlayer(pid)
    addLog('RES', `201 Created — SR: ${ticket.sr}`, 'response')
    addToQueued(pid)
    secondPlayerSelect.value = null
    addLog('SSE', `⏳ QueueService now searching SR matches for both players…`, 'event')
  } catch (e) {
    addLog('ERR', `2nd player enqueue failed: ${e.message}`, 'error')
  }
}

// ─── Cancel Queue ────────────────────────────────────
async function cancelQueue() {
  stopQueuePolling()
  if (currentStream.value) {
    currentStream.value.close()
    currentStream.value = null
  }
  if (selectedPlayerId.value) {
    addLog('REQ', `DELETE /queue/${shortId(selectedPlayerId.value)}`, 'request')
    try {
      await dequeuePlayer(selectedPlayerId.value)
      addLog('RES', '204 No Content', 'response')
    } catch (_) { /* ignore */ }
    removeFromQueued(selectedPlayerId.value)
  }
  flowState.value = 'idle'
}

// ─── Game Server Simulation ──────────────────────────
async function simulateGameEnd() {
  if (!selectedWinner.value || !currentMatch.value) return

  const match = currentMatch.value
  const winnerId = selectedWinner.value
  const isDraw = winnerId === 'draw'
  const winner = isDraw ? 'draw' : winnerId
  const winnerName = isDraw ? 'Draw' : getPlayerName(winnerId)
  const result = isDraw ? 'draw' : `${winnerName} wins`

  addLog('REQ', `PATCH /matches/${shortId(match.id)} { winner: "${isDraw ? 'draw' : shortId(winnerId)}", result: "${result}" }`, 'request')

  try {
    const res = await submitMatchResult(match.id, winner, result)
    addLog('RES', `200 OK — status: ${res.status}`, 'response')

    // Explain event-driven post-match flow from §2.8
    addLog('SSE', '📡 Event-driven post-match flow triggered:', 'event')
    addLog('SSE', '  → MatchService publishes MatchUpdatedEvent to Broker', 'event')
    addLog('SSE', '  → Broker → RatingService: MatchEnded consumer', 'event')
    addLog('SSE', '  → RatingService: Glicko-2 recalculate per player', 'event')
    addLog('SSE', '  → RatingService publishes RatingUpdatedEvent to Broker', 'event')
    addLog('SSE', '  → Broker → MatchmakingProcessService: receives both events', 'event')

    flowState.value = 'finished'
    selectedWinner.value = null

    // Refresh match
    const updated = await fetchMatch(match.id)
    if (updated) currentMatch.value = updated

    // Refresh ratings after event processing
    setTimeout(async () => {
      addLog('REQ', 'Refreshing player ratings…', 'request')
      for (const pid of matchPlayers.value) {
        try {
          const r = await fetchRating(pid)
          if (r) {
            const oldRating = playerRatings[pid]?.rating
            playerRatings[pid] = r
            if (oldRating != null && oldRating !== r.rating) {
              const delta = (r.rating - oldRating).toFixed(1)
              const arrow = r.rating > oldRating ? '📈' : '📉'
              addLog('RES', `${arrow} ${getPlayerName(pid)}: ${oldRating} → ${r.rating} (${delta > 0 ? '+' : ''}${delta})`, 'response')
            } else {
              addLog('RES', `${getPlayerName(pid)}: rating ${r.rating} (unchanged — event may still be processing)`, 'response')
            }
          }
        } catch (_) { /* skip */ }
      }
    }, 2500)
  } catch (e) {
    addLog('ERR', e.message, 'error')
  }
}

// ─── Reset ───────────────────────────────────────────
function resetFlow() {
  stopQueuePolling()
  flowState.value = 'idle'
  currentMatchId.value = null
  currentMatch.value = null
  matchPlayers.value = []
  selectedWinner.value = null
  secondPlayerSelect.value = null
  if (currentStream.value) {
    currentStream.value.close()
    currentStream.value = null
  }
}

// ─── Init ────────────────────────────────────────────
onMounted(() => {
  loadPlayers()
  pollQueuedPlayers()
})

onUnmounted(() => {
  stopQueuePolling()
  if (currentStream.value) currentStream.value.close()
})
</script>

<template>
  <div>
    <header class="app-header">
      <div>
        <h1>⚡ Glicko-2 Matchmaking System</h1>
        <span class="subtitle">Business Process Simulation — analysis-and-design.md §2.8</span>
      </div>
      <div style="margin-left:auto;display:flex;gap:8px;align-items:center;">
        <span v-if="queuedPlayerIds.length > 0" class="badge badge-queue">
          {{ queuedPlayerIds.length }} in queue
        </span>
        <button class="btn btn-sm btn-primary" @click="loadPlayers">↻ Refresh</button>
      </div>
    </header>

    <div class="app-layout">

      <!-- ═══ LEFT: Players ═══ -->
      <div class="panel">
        <div class="panel-header">
          <h2>👤 Players</h2>
          <span class="badge badge-rating">{{ players.length }}</span>
        </div>
        <div class="panel-body">
          <div class="flow-label">Select a player to start matchmaking</div>

          <!-- Queued players section -->
          <div v-if="queuedPlayerIds.length > 0" style="margin-bottom:16px;">
            <div class="flow-label" style="color:var(--info);">🔄 Currently in Queue ({{ queuedPlayerIds.length }})</div>
            <div
              v-for="pid in queuedPlayerIds" :key="'q-'+pid"
              class="card queued" style="cursor:default;"
            >
              <div class="card-row">
                <div>
                  <div class="card-name">{{ getPlayerName(pid) }}</div>
                  <div class="card-id">{{ shortId(pid) }}</div>
                </div>
                <div style="text-align:right;">
                  <span class="badge badge-queue pulse">QUEUED</span>
                </div>
              </div>
            </div>
          </div>

          <!-- All players list -->
          <div class="flow-label">All Players</div>
          <div
            v-for="p in players" :key="p.id"
            class="card"
            :class="{
              active: selectedPlayerId === p.id && flowState === 'idle',
              queued: isQueued(p.id),
              matched: matchPlayers.includes(p.id),
            }"
            @click="flowState === 'idle' ? (selectedPlayerId = p.id) : null"
          >
            <div class="card-row">
              <div>
                <div class="card-name">{{ p.name }}</div>
                <div class="card-id">{{ shortId(p.id) }}</div>
              </div>
              <div style="text-align:right;">
                <span v-if="playerRatings[p.id]" class="badge badge-rating">
                  {{ Math.round(playerRatings[p.id].rating) }} SR
                </span>
                <span v-if="isQueued(p.id)" class="badge badge-queue" style="margin-left:4px;">Q</span>
                <span v-if="matchPlayers.includes(p.id)" class="badge badge-finished" style="margin-left:4px;">M</span>
              </div>
            </div>
          </div>

          <div v-if="players.length === 0" class="empty-state">
            <div class="empty-icon">⏳</div>
            <p>Loading players…<br>Make sure backend services are running.</p>
          </div>
        </div>
      </div>

      <!-- ═══ CENTER: Matchmaking Flow ═══ -->
      <div class="panel">
        <div class="panel-header">
          <h2>🎮 Matchmaking Flow</h2>
          <span class="badge" :class="{
            'badge-rating': flowState === 'idle',
            'badge-queue': flowState === 'queuing' || flowState === 'searching',
            'badge-pending': flowState === 'matched' || flowState === 'playing',
            'badge-finished': flowState === 'finished',
          }">{{ flowState.toUpperCase() }}</span>
        </div>
        <div class="panel-body">

          <!-- IDLE -->
          <div v-if="flowState === 'idle'" class="flow-section">
            <div class="flow-label">Step 1 — Player Enqueues for Match</div>
            <div v-if="!selectedPlayerId" class="flow-status">
              <div class="status-icon">👈</div>
              <div class="status-text">Select a player</div>
              <div class="status-sub">Click a player card on the left panel</div>
            </div>
            <div v-else>
              <div class="flow-status" style="margin-bottom:12px;">
                <div class="status-icon">⚔️</div>
                <div class="status-text">{{ selectedPlayer?.name }}</div>
                <div class="status-sub">
                  SR: {{ playerRatings[selectedPlayerId]?.rating ?? '—' }}
                  · RD: {{ playerRatings[selectedPlayerId]?.rd ?? '—' }}
                </div>
              </div>
              <button class="btn btn-primary btn-block" @click="startMatchmaking" :disabled="isQueued(selectedPlayerId)">
                🔍 Find Match
              </button>
              <div class="status-sub" style="text-align:center;margin-top:6px;font-family:'JetBrains Mono',monospace;">
                POST /queue → GET /queue/{playerId}/stream (SSE)
              </div>
            </div>
          </div>

          <!-- SEARCHING -->
          <div v-if="flowState === 'queuing' || flowState === 'searching'" class="flow-section">
            <div class="flow-label">Step 2 — Searching for Opponents (SSE Stream Active)</div>
            <div class="flow-status">
              <div><span class="spinner"></span></div>
              <div class="status-text pulse" style="margin-top:12px;">Searching for opponents…</div>
              <div class="status-sub" style="font-family:'JetBrains Mono',monospace;">
                SSE: GET /queue/{{ shortId(selectedPlayerId) }}/stream
              </div>
              <div class="status-sub" style="margin-top:4px;">
                QueueProcessService is scanning tickets within SR ±100 range
              </div>
            </div>

            <button class="btn btn-danger btn-block" style="margin-top:12px;" @click="cancelQueue">
              ✕ Cancel Queue
            </button>

            <!-- Enqueue 2nd player helper -->
            <div style="margin-top:16px;padding:14px;background:var(--bg-card);border-radius:var(--radius);border:1px solid var(--border);">
              <div class="flow-label" style="color:var(--success);">💡 Enqueue a 2nd player to trigger matchmaking</div>
              <div style="font-size:12px;color:var(--text-secondary);margin-bottom:8px;">
                The QueueProcessService will auto-detect two compatible players and create a match via MatchmakingProcessService.
              </div>
              <select v-model="secondPlayerSelect" style="margin-bottom:8px;">
                <option :value="null">— Select 2nd player —</option>
                <option v-for="p in availableSecondPlayers" :key="p.id" :value="p.id">
                  {{ p.name }} (SR: {{ Math.round(playerRatings[p.id]?.rating ?? 0) }})
                </option>
              </select>
              <button
                class="btn btn-success btn-sm btn-block"
                :disabled="!secondPlayerSelect"
                @click="enqueueSecondPlayer"
              >
                ➕ Enqueue 2nd Player — POST /queue
              </button>
            </div>

            <!-- Show who's in queue -->
            <div v-if="queuedPlayerIds.length > 0" style="margin-top:16px;">
              <div class="flow-label">Players currently in queue</div>
              <div v-for="pid in queuedPlayerIds" :key="'qs-'+pid" class="card queued" style="cursor:default;">
                <div class="card-row">
                  <span class="card-name">{{ getPlayerName(pid) }}</span>
                  <span class="badge badge-queue">SR {{ Math.round(playerRatings[pid]?.rating ?? 0) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- MATCHED -->
          <div v-if="flowState === 'matched'" class="flow-section">
            <div class="flow-label">Step 3 — Match Created</div>
            <div class="flow-status" style="border-color:var(--success);">
              <div class="status-icon">🎯</div>
              <div class="status-text" style="color:var(--success);">Match Found!</div>
              <div class="status-sub" style="font-family:'JetBrains Mono',monospace;">
                matchId: {{ shortId(currentMatchId) }}
              </div>
            </div>
            <div style="margin-top:12px;">
              <div v-for="pid in matchPlayers" :key="pid" class="card matched" style="cursor:default;">
                <div class="card-row">
                  <span class="card-name">{{ getPlayerName(pid) }}</span>
                  <span class="badge badge-rating">{{ Math.round(playerRatings[pid]?.rating ?? 0) }} SR</span>
                </div>
              </div>
            </div>
          </div>

          <!-- PLAYING — GameServer Sim -->
          <div v-if="flowState === 'playing'" class="flow-section">
            <div class="flow-label">Step 4 — Game Server Simulation</div>
            <div class="flow-status" style="border-color:var(--warning);">
              <div class="status-icon">🕹️</div>
              <div class="status-text" style="color:var(--warning);">In-Game Session (Out of Scope)</div>
              <div class="status-sub">Design note: "In-game session is out of scope and runs externally"</div>
            </div>

            <div class="match-card" style="margin-top:16px;">
              <div class="match-id">Match: {{ currentMatchId }}</div>
              <div class="match-players">
                <div v-for="pid in matchPlayers" :key="pid" class="match-player">
                  🎮 {{ getPlayerName(pid) }} — SR {{ Math.round(playerRatings[pid]?.rating ?? 0) }}
                </div>
              </div>
              <div class="flow-label">Select match outcome</div>
              <select v-model="selectedWinner" style="margin-bottom:10px;">
                <option :value="null">— Select winner —</option>
                <option v-for="pid in matchPlayers" :key="pid" :value="pid">
                  🏆 {{ getPlayerName(pid) }} wins
                </option>
                <option value="draw">🤝 Draw</option>
              </select>
              <button class="btn btn-success btn-block" :disabled="!selectedWinner" @click="simulateGameEnd">
                📤 Submit Result — PATCH /matches/{{ shortId(currentMatchId) }}
              </button>
              <div class="status-sub" style="text-align:center;margin-top:8px;">
                This triggers: MatchUpdatedEvent → Broker → RatingService (Glicko-2) → RatingUpdatedEvent
              </div>
            </div>
          </div>

          <!-- FINISHED -->
          <div v-if="flowState === 'finished'" class="flow-section">
            <div class="flow-label">Step 5 — Process Complete</div>
            <div class="flow-status" style="border-color:var(--success);">
              <div class="status-icon">✅</div>
              <div class="status-text" style="color:var(--success);">Match Complete</div>
              <div class="status-sub">{{ currentMatch?.result || '—' }}</div>
            </div>
            <div style="margin-top:16px;">
              <div class="flow-label">Post-Match Player Ratings</div>
              <div v-for="pid in matchPlayers" :key="pid" class="card" style="cursor:default;">
                <div class="card-row">
                  <span class="card-name">{{ getPlayerName(pid) }}</span>
                  <span class="badge badge-rating">{{ playerRatings[pid]?.rating ?? '…' }}</span>
                </div>
                <div class="card-id" style="margin-top:4px;">
                  RD: {{ playerRatings[pid]?.rd ?? '—' }} · Vol: {{ playerRatings[pid]?.volatility ?? '—' }}
                </div>
              </div>
            </div>
            <button class="btn btn-primary btn-block" style="margin-top:16px;" @click="resetFlow">
              🔄 Start New Match
            </button>
          </div>

          <!-- Flow Reference -->
          <div style="margin-top:24px;padding:14px;background:var(--bg-card);border-radius:var(--radius);border:1px solid var(--border);">
            <div class="flow-label">📐 Design §2.8 — Service Composition Flow</div>
            <div style="font-size:11px;color:var(--text-muted);font-family:'JetBrains Mono',monospace;line-height:1.7;">
              <div>1. Client → POST /queue (enqueue)</div>
              <div>2. Queue → PlayerService: verify player exists</div>
              <div>3. Queue → RatingService: GET rating (SR snapshot)</div>
              <div>4. Client ← SSE /queue/{id}/stream</div>
              <div>5. Queue → MatchmakingProcess: POST /mm</div>
              <div>6. MatchmakingProcess → MatchService: POST /matches</div>
              <div>7. MatchmakingProcess → Queue: DELETE /queue</div>
              <div>8. MatchmakingProcess → Broker: MatchReady</div>
              <div style="color:var(--warning);">9. Client receives MATCH_FOUND via SSE</div>
              <div style="color:var(--warning);">10. GameServer → PATCH /matches/{id} (result)</div>
              <div style="color:var(--success);">11. Broker → RatingService: Glicko-2 recalculate</div>
              <div style="color:var(--success);">12. Broker → MatchService: update match status</div>
            </div>
          </div>
        </div>
      </div>

      <!-- ═══ RIGHT: Activity Log ═══ -->
      <div class="panel">
        <div class="panel-header">
          <h2>📋 Activity Log</h2>
          <button class="btn btn-sm btn-danger" @click="logs = []" style="padding:4px 10px;">Clear</button>
        </div>
        <div class="panel-body">
          <div v-if="logs.length === 0" class="empty-state">
            <div class="empty-icon">📋</div>
            <p>Start a matchmaking flow to see activity</p>
          </div>
          <div
            v-for="entry in logs" :key="entry.id"
            class="log-entry"
            :class="{
              'log-request': entry.type === 'request',
              'log-response': entry.type === 'response',
              'log-event': entry.type === 'event',
              'log-error': entry.type === 'error',
            }"
          >
            <span class="log-time">{{ entry.time }}</span>
            <span class="log-tag" :class="{
              'tag-req': entry.tag === 'REQ',
              'tag-res': entry.tag === 'RES',
              'tag-sse': entry.tag === 'SSE',
              'tag-err': entry.tag === 'ERR',
            }">{{ entry.tag }}</span>
            {{ entry.message }}
          </div>
        </div>
      </div>

    </div>
  </div>
</template>
