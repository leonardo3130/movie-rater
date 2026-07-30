import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteWatchSession } from '../../../api/endpoints/watch-sessions'
import { toast } from 'sonner'

export function useDeleteWatchSession() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteWatchSession,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['watch-sessions'] })
      toast.success('Watch session deleted')
    },
    onError: () => {
      toast.error('Failed to delete watch session')
    },
  })
}