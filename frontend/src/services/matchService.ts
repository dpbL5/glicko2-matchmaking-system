import axios from 'axios';
import type { Match, MatchCreateRequest, MatchResult, MatchResultResponse, HealthResponse } from '@/types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_GATEWAY || '/api',
});

export const matchService = {
  async getHealth(): Promise<HealthResponse> {
    const response = await api.get('/matches/health');
    return response.data;
  },

  async createMatch(request: MatchCreateRequest): Promise<Match> {
    const response = await api.post('/matches', request);
    return response.data;
  },

  async getMatch(id: string): Promise<Match> {
    const response = await api.get(`/matches/${id}`);
    return response.data;
  },

  async submitMatchResult(matchId: string, result: MatchResult): Promise<MatchResultResponse> {
    const response = await api.post(`/matches/${matchId}/result`, result);
    return response.data;
  },
};
