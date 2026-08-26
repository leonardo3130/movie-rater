import { useQuery } from '@tanstack/react-query'
import { getDashboard } from '../../../api/endpoints/dashboard'

export function useDashboard(groupId: string | null) {
  return useQuery({
    queryKey: ['dashboard', groupId],
    queryFn: () => getDashboard(groupId!),
    staleTime: 30_000,
    enabled: !!groupId
  })
}
