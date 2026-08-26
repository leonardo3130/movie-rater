import client from '../client'
import type { DashboardResponseDto } from '@src/types/dashboard'

export function getDashboard(gid: string) {
  return client.get<DashboardResponseDto>(`/api/dashboard/${gid}`).then((r) => r.data)
}
