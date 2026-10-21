import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createWatchSession } from '../../../api/endpoints/watch-sessions'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

export function useCreateWatchSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createWatchSession,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('movies.toast.createSessionFailed')))
    },
  })
}