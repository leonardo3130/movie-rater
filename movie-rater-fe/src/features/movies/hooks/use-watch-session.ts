import { useQuery } from '@tanstack/react-query'
import { getWatchSession } from '../../../api/endpoints/watch-sessions'

export function useWatchSession(id: string | undefined) {
  return useQuery({
    queryKey: ['watch-sessions', id],
    queryFn: () => getWatchSession(id!),
    enabled: !!id,
  })
}