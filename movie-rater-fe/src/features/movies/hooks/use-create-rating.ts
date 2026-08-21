import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createRating } from '../../../api/endpoints/ratings'
import { toast } from 'sonner'

export function useCreateRating() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ watchSessionId, ...data }: { watchSessionId: string; ratingValue: number; review?: string | null }) =>
      createRating(watchSessionId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions', variables.watchSessionId] })
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      toast.success('Rating submitted')
    },
    onError: () => {
      toast.error('Failed to submit rating')
    },
  })
}
