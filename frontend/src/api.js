const BASE = '/api'

export async function fetchPlayers() {
  const res = await fetch(`${BASE}/players`)
  if (!res.ok) throw new Error(`Failed to fetch players: ${res.status}`)
  return res.json()
}

export async function fetchRating(playerId) {
  const res = await fetch(`${BASE}/ratings/${playerId}`)
  if (!res.ok) return null
  return res.json()
}

export async function enqueuePlayer(playerId) {
  const res = await fetch(`${BASE}/queue`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ playerId }),
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({}))
    throw new Error(err.title || `Enqueue failed: ${res.status}`)
  }
  return res.json()
}

export function subscribeQueueStream(playerId, onEvent, onError) {
  const es = new EventSource(`${BASE}/queue/${playerId}/stream`)

  es.addEventListener('stream-open', (e) => {
    onEvent({ type: 'stream-open', data: JSON.parse(e.data) })
  })

  es.addEventListener('queue-signal', (e) => {
    onEvent({ type: 'queue-signal', data: JSON.parse(e.data) })
  })

  es.onerror = (e) => {
    if (es.readyState === EventSource.CLOSED) {
      onEvent({ type: 'stream-closed', data: null })
    } else {
      onError?.(e)
    }
    es.close()
  }

  return es
}

export async function dequeuePlayer(playerId) {
  const res = await fetch(`${BASE}/queue/${playerId}`, { method: 'DELETE' })
  return res.ok
}

export async function fetchMatch(matchId) {
  const res = await fetch(`${BASE}/matches/${matchId}`)
  if (!res.ok) return null
  return res.json()
}

export function subscribeMatchByIdStream(matchId, onEvent, onError) {
  const es = new EventSource(`${BASE}/matches/${matchId}`)

  es.addEventListener('stream-open', (e) => {
    onEvent({ type: 'stream-open', data: JSON.parse(e.data) })
  })

  es.addEventListener('match-found', (e) => {
    onEvent({ type: 'match-found', data: JSON.parse(e.data) })
  })

  es.onerror = (e) => {
    onError?.(e)
    es.close()
  }

  return es
}

export async function submitMatchResult(matchId, winner, result) {
  const res = await fetch(`${BASE}/matches/${matchId}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ winner, result }),
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({}))
    throw new Error(err.title || `Submit result failed: ${res.status}`)
  }
  return res.json()
}

export async function fetchQueuedPlayers() {
  const res = await fetch(`${BASE}/queue/players`)
  if (!res.ok) return []
  return res.json()
}
