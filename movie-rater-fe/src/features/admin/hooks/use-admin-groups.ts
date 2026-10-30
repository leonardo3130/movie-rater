import { useQuery } from '@tanstack/react-query'
import { getAdminGroups } from '@src/api/endpoints/admin'

export function useAdminGroups() {
  return useQuery({
    queryKey: ['admin', 'groups'],
    queryFn: getAdminGroups,
    staleTime: 30_000,
  })
}
