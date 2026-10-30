import { useQuery } from '@tanstack/react-query'
import { getAdminOverview } from '@src/api/endpoints/admin'

export function useAdminOverview() {
  return useQuery({
    queryKey: ['admin', 'overview'],
    queryFn: () => getAdminOverview(),
    staleTime: 60_000,
  })
}
