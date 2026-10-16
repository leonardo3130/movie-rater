import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateWatchSession } from '../../../api/endpoints/watch-sessions'
import { toast } from 'sonner'
import type { UpdateWatchSessionRequestDto } from '@src/types/watch-session'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

export function useUpdateWatchSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateWatchSessionRequestDto }) =>
      updateWatchSession(id, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions', variables.id] })
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast.success(t('movies.toast.sessionUpdated'))
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('movies.toast.updateSessionFailed')))
    },
  })
}