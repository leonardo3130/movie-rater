import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { getAdminInvitations } from '@src/api/endpoints/admin'
import type { AdminInvitationQuery } from '@src/types/admin'

export function useAdminInvitations(query: AdminInvitationQuery) {
  return useQuery({
    queryKey: ['admin', 'invitations', query],
    queryFn: () => getAdminInvitations(query),
    staleTime: 15_000,
    placeholderData: keepPreviousData,
  })
}
