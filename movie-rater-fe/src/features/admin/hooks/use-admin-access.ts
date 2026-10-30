import { useQuery } from '@tanstack/react-query'
import { getAdminAccess } from '@src/api/endpoints/admin'
import { useAuthStore } from '@src/stores/auth-store'

export function useAdminAccess() {
  const userId = useAuthStore((s) => s.user?.id)

  // Keyed by user so switching accounts never reuses another user's answer.
  return useQuery({
    queryKey: ['admin', 'access', userId],
    queryFn: getAdminAccess,
    staleTime: Infinity,
    enabled: !!userId,
  })
}
