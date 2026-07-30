import client from '../client'
import type { DashboardResponseDto } from '@src/types/dashboard'

export function getDashboard() {
  return client.get<DashboardResponseDto>('/api/dashboard').then((r) => r.data)
}