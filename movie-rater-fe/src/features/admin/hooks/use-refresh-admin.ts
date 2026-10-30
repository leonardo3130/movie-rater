import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getAdminActivity, getAdminOverview } from '@src/api/endpoints/admin'
import { getApiErrorMessage } from '@src/api/errors'
import { t } from '@src/stores/locale-store'

// Recomputes overview/activity on the API (bypassing its 5 minute cache) and refetches
// the uncached lists.
export function useRefreshAdmin(activityDays: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
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
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('admin.refreshFailed')))
    },
  })
}
