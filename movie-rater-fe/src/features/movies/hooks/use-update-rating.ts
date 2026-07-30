import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateRating } from '../../../api/endpoints/ratings'
import { toast } from 'sonner'

export function useUpdateRating() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ watchSessionId, ...data }: { watchSessionId: string; ratingValue: number; review?: string | null }) =>
      updateRating(watchSessionId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions', variables.watchSessionId] })
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      toast.success('Rating updated')
    },
    onError: () => {
      toast.error('Failed to update rating')
    },
  })
}