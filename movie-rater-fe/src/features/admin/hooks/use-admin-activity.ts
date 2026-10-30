import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { getAdminActivity } from '@src/api/endpoints/admin'

export function useAdminActivity(days: number) {
  return useQuery({
    queryKey: ['admin', 'activity', days],
    queryFn: () => getAdminActivity(days),
    staleTime: 60_000,
    placeholderData: keepPreviousData,
  })
}
