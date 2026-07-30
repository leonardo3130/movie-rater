import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createWatchSession } from '../../../api/endpoints/watch-sessions'
import { toast } from 'sonner'

export function useCreateWatchSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createWatchSession,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
    },
    onError: () => {
      toast.error('Failed to create watch session')
    },
  })
}