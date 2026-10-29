import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getAdminAccess,
  getAdminActivity,
  getAdminGroups,
  getAdminInvitations,
  getAdminOverview,
} from '@src/api/endpoints/admin'
import type { AdminInvitationQuery } from '@src/types/admin'
import { useAuthStore } from '@src/stores/auth-store'

export function useAdminAccess() {
  const userId = useAuthStore((s) => s.user?.id)

  return useQuery({
    queryKey: ['admin', 'access', userId],
    queryFn: getAdminAccess,
    staleTime: Infinity,
    enabled: !!userId,
  })
}

export function useAdminOverview() {
  return useQuery({
    queryKey: ['admin', 'overview'],
    queryFn: () => getAdminOverview(),
    staleTime: 60_000,
  })
}

export function useAdminActivity(days: number) {
  return useQuery({
    queryKey: ['admin', 'activity', days],
    queryFn: () => getAdminActivity(days),
    staleTime: 60_000,
  })
}

export function useAdminGroups() {
  return useQuery({
    queryKey: ['admin', 'groups'],
    queryFn: getAdminGroups,
    staleTime: 30_000,
  })
}

export function useAdminInvitations(query: AdminInvitationQuery) {
  return useQuery({
    queryKey: ['admin', 'invitations', query],
    queryFn: () => getAdminInvitations(query),
    staleTime: 15_000,
    placeholderData: (previous) => previous,
  })
}

// Bypasses the API's 5 minute cache for overview/activity and refetches every admin query.
export function useRefreshAdmin() {
  const queryClient = useQueryClient()

  return async (activityDays: number) => {
    const [overview, activity] = await Promise.all([
      getAdminOverview(true),
      getAdminActivity(activityDays, true),
    ])
    queryClient.setQueryData(['admin', 'overview'], overview)
    queryClient.setQueryData(['admin', 'activity', activityDays], activity)
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['admin', 'groups'] }),
      queryClient.invalidateQueries({ queryKey: ['admin', 'invitations'] }),
    ])
  }
}
