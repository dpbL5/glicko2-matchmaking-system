import axios from 'axios';
import type { PlayerRating, RatingUpdateRequest, RatingRecalculateRequest, HealthResponse } from '@/types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_GATEWAY || '/api',
});

export const ratingService = {
  async getHealth(): Promise<HealthResponse> {
    const response = await api.get('/ratings/health');
    return response.data;
  },

  async getPlayerRating(playerId: string): Promise<PlayerRating> {
    const response = await api.get(`/ratings/${playerId}`);
    return response.data;
  },

  async updatePlayerRating(playerId: string, request: RatingUpdateRequest): Promise<PlayerRating> {
    const response = await api.post(`/ratings/${playerId}`, request);
    return response.data;
  },

  async recalculatePlayerRating(playerId: string, request: RatingRecalculateRequest): Promise<PlayerRating> {
    const response = await api.post(`/ratings/${playerId}/recalculate`, request);
    return response.data;
  },
};
