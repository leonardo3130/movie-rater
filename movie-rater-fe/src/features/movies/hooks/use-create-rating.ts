import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createRating } from '../../../api/endpoints/ratings'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

export function useCreateRating() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ watchSessionId, ...data }: { watchSessionId: string; ratingValue: number; review?: string | null }) =>
      createRating(watchSessionId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions', variables.watchSessionId] })
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      toast.success(t('movies.toast.ratingSubmitted'))
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('movies.toast.submitRatingFailed')))
    },
  })
}
