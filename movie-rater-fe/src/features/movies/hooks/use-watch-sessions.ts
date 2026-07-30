import { useQuery } from '@tanstack/react-query'
import { getWatchSessions } from '../../../api/endpoints/watch-sessions'
import type { WatchSessionQueryDto } from '@src/types/watch-session'

export function useWatchSessions(params: WatchSessionQueryDto) {
  return useQuery({
    queryKey: ['watch-sessions', params],
    queryFn: () => getWatchSessions(params),
  })
}