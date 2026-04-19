// Player Service types
export interface Player {
  id: string;
  name: string;
}

// Queue Service types
export interface QueueTicket {
  playerId: string;
  sr: number;
  status: string;
  queuedAt: string;
  matchedAt: string | null;
}

export interface QueueEnqueueRequest {
  playerId: string;
}

// Match Service types
export interface Match {
  id: string;
  playerIds: string[];
  status: string;
}

export interface MatchCreateRequest {
  playerIds: string[];
  queueId?: string | null;
}

export interface MatchResult {
  winner: string;
  result: string;
}

export interface MatchResultResponse {
  matchId: string;
  status: string;
}

// Matchmaking Process Service types
export interface MatchInitRequest {
  playerIds: string[];
  queueId?: string | null;
}

export interface MatchInitResult {
  matchId: string;
  status: string;
  playerIds: string[];
  dequeuedPlayerIds: string[];
  failedToDequeuePlayerIds: string[];
}

// Rating Service types
export interface PlayerRating {
  playerId: string;
  rating: number;
  rd: number;
  volatility: number;
}

export interface RatingUpdateRequest {
  rating: number;
  rd: number;
  volatility: number;
}

export interface RatingRecalculateRequest {
  matchResult: 'win' | 'draw' | 'loss';
  opponentRatings: number[];
}

// System types
export interface HealthResponse {
  status: string;
}

export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
}
