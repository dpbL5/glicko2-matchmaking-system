import axios from 'axios';
import type { QueueTicket, QueueEnqueueRequest, HealthResponse } from '@/types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_GATEWAY || '/api',
});

export const queueService = {
  async getHealth(): Promise<HealthResponse> {
    const response = await api.get('/queue/health');
    return response.data;
  },

  async enqueue(request: QueueEnqueueRequest): Promise<QueueTicket> {
    const response = await api.post('/queue', request);
    return response.data;
  },

  async getQueueStatus(playerId: string): Promise<QueueTicket> {
    const response = await api.get(`/queue/${playerId}`);
    return response.data;
  },

  async removeFromQueue(playerId: string): Promise<void> {
    await api.delete(`/queue/${playerId}`);
  },

  streamQueueUpdates(playerId: string, onMessage: (data: unknown) => void, onError?: (error: Event) => void): EventSource {
    const url = `${api.defaults.baseURL}/queue/${playerId}/stream`;
    const source = new EventSource(url);

    const handleMessage = (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        onMessage(data);
      } catch (e) {
        console.error('Failed to parse SSE message:', e);
      }
    };

    source.onmessage = handleMessage;
    source.addEventListener('queue-status', (event) => handleMessage(event as MessageEvent));

    if (onError) {
      source.onerror = onError;
    }

    return source;
  },
};
