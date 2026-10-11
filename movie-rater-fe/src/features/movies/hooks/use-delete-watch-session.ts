import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteWatchSession } from '../../../api/endpoints/watch-sessions'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'

export function useDeleteWatchSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteWatchSession,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      toast.success(t('movies.toast.sessionDeleted'))
    },
    onError: () => {
      toast.error(t('movies.toast.deleteSessionFailed'))
    },
  })
}