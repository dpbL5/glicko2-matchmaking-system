import axios from 'axios';
import type { HealthResponse, MatchInitRequest, MatchInitResult } from '@/types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_GATEWAY || '/api',
});

export const matchmakingProcessService = {
  async getHealth(): Promise<HealthResponse> {
    const response = await api.get('/mm/health');
    return response.data;
  },

  async initializeMatchmaking(request: MatchInitRequest): Promise<MatchInitResult> {
    const response = await api.post('/mm', request);
    return response.data;
  },
};
