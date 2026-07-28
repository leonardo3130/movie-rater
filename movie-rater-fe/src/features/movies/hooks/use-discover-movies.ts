import { useQuery } from '@tanstack/react-query'
import { discoverMovies } from '../../../api/endpoints/movies'
import type { DiscoverFilters } from '@src/types/movie'

export function useDiscoverMovies(filters: DiscoverFilters, page: number) {
  const genreIds = filters.genreIds.length > 0 ? filters.genreIds.join(',') : undefined

  return useQuery({
    queryKey: ['movies', 'discover', filters, page],
    queryFn: () =>
      discoverMovies({
        page,
        genreIds,
        primaryReleaseYear: filters.primaryReleaseYear || undefined,
        primaryReleaseDateGte: filters.primaryReleaseDateGte || undefined,
        primaryReleaseDateLte: filters.primaryReleaseDateLte || undefined,
        sortBy: filters.sortBy,
        voteAverageGte: filters.voteAverageGte || undefined,
        includeAdult: filters.includeAdult || undefined,
      }),
  })
}