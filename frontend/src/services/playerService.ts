import axios from 'axios';
import type { Player, HealthResponse } from '@/types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_GATEWAY || '/api',
});

export const playerService = {
  async getHealth(): Promise<HealthResponse> {
    const response = await api.get('/players/health');
    return response.data;
  },

  async listPlayers(): Promise<Player[]> {
    const response = await api.get('/players');
    return response.data;
  },

  async getPlayer(id: string): Promise<Player> {
    const response = await api.get(`/players/${id}`);
    return response.data;
  },
};
