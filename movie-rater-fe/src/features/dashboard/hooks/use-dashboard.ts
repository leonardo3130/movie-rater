import { useQuery } from '@tanstack/react-query'
import { getDashboard } from '../../../api/endpoints/dashboard'

export function useDashboard() {
  return useQuery({
    queryKey: ['dashboard'],
    queryFn: getDashboard,
    staleTime: 30_000,
  })
}