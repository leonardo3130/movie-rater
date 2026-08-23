import client from '../client'
import type {
  WatchSessionResponseDto,
  WatchSessionListResponseDto,
  CreateWatchSessionRequestDto,
  WatchSessionQueryDto,
  HeatmapResponseDto,
} from '@src/types/watch-session'

export function createWatchSession(data: CreateWatchSessionRequestDto) {
  return client.post<WatchSessionResponseDto>('/api/watch-sessions', data).then((r) => r.data)
}

export function getWatchSessions(params: WatchSessionQueryDto = {}) {
  const qp = new URLSearchParams()
  if (params.movieId) qp.set('movieId', params.movieId)
  if (params.groupId) qp.set('groupId', params.groupId)
  if (params.page) qp.set('page', String(params.page))
  if (params.pageSize) qp.set('pageSize', String(params.pageSize))
  const qs = qp.toString()
  return client
    .get<WatchSessionListResponseDto>(`/api/watch-sessions${qs ? `?${qs}` : ''}`)
    .then((r) => r.data)
}

export function getWatchSession(id: string) {
  return client.get<WatchSessionResponseDto>(`/api/watch-sessions/${id}`).then((r) => r.data)
}

export function deleteWatchSession(id: string) {
  return client.delete(`/api/watch-sessions/${id}`)
}

export function getHeatmap(days = 365) {
  return client
    .get<HeatmapResponseDto>(`/api/watch-sessions/heatmap?days=${days}`)
    .then((r) => r.data)
}
