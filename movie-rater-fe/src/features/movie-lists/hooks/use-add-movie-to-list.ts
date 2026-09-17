import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addMovieToList } from '../../../api/endpoints/movie-lists'
import { getMovieDetails } from '../../../api/endpoints/movies'
import type { ApiError } from '@src/types/auth'
import { toast } from 'sonner'

const ZERO_GUID = '00000000-0000-0000-0000-000000000000'

function resolveMovieId(movieId: string, tmdbId: number): Promise<string> {
  if (movieId && movieId !== ZERO_GUID) return Promise.resolve(movieId)
  return getMovieDetails(tmdbId).then((movie) => movie.id)
}

interface AddMovieToListInput {
  listId: string
  movieId: string
  tmdbId: number
}

export function useAddMovieToList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ listId, movieId, tmdbId }: AddMovieToListInput) =>
      resolveMovieId(movieId, tmdbId).then((id) => addMovieToList(listId, id)),
    onSuccess: (_data, { listId }) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      queryClient.invalidateQueries({ queryKey: ['movie-list', listId] })
      toast.success('Movie added to list')
    },
    onError: (error: unknown) => {
      const apiError = error as { response?: { data?: ApiError } }
      const status = apiError?.response?.data?.status
      if (status === 409) {
        toast.error('This movie is already in the selected list')
      } else {
        toast.error('Failed to add movie to list')
      }
    },
  })
}