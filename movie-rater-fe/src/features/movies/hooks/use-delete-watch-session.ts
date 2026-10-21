import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteWatchSession } from '../../../api/endpoints/watch-sessions'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

export function useDeleteWatchSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteWatchSession,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast.success(t('movies.toast.sessionDeleted'))
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('movies.toast.deleteSessionFailed')))
    },
  })
}