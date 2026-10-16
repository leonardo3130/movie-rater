import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateRating } from '../../../api/endpoints/ratings'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

export function useUpdateRating() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ watchSessionId, ...data }: { watchSessionId: string; ratingValue: number; review?: string | null }) =>
      updateRating(watchSessionId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions', variables.watchSessionId] })
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      toast.success(t('movies.toast.ratingUpdated'))
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('movies.toast.updateRatingFailed')))
    },
  })
}