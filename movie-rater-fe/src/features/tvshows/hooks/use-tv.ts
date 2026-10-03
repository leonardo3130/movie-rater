import { useQuery } from '@tanstack/react-query'
import {
  searchTvShows,
  getTvGenres,
  getTvShowDetails,
  getTvSeason,
  getTvEpisode,
} from '../../../api/endpoints/tv'

export function useSearchTv(query: string, page: number) {
  return useQuery({
    queryKey: ['tv', 'search', query, page],
    queryFn: () => searchTvShows({ query, page }),
    enabled: query.trim().length > 0,
  })
}

export function useTvGenres() {
  return useQuery({
    queryKey: ['tv', 'genres'],
    queryFn: () => getTvGenres(),
  })
}

export function useTvShowDetails(tmdbId: number | null) {
  return useQuery({
    queryKey: ['tv', 'show', tmdbId],
    queryFn: () => getTvShowDetails(tmdbId!),
    enabled: tmdbId !== null,
  })
}

export function useTvSeason(tmdbId: number | null, seasonNumber: number | null) {
  return useQuery({
    queryKey: ['tv', 'season', tmdbId, seasonNumber],
    queryFn: () => getTvSeason(tmdbId!, seasonNumber!),
    enabled: tmdbId !== null && seasonNumber !== null,
  })
}

export function useTvEpisode(
  tmdbId: number | null,
  seasonNumber: number | null,
  episodeNumber: number | null,
) {
  return useQuery({
    queryKey: ['tv', 'episode', tmdbId, seasonNumber, episodeNumber],
    queryFn: () => getTvEpisode(tmdbId!, seasonNumber!, episodeNumber!),
    enabled: tmdbId !== null && seasonNumber !== null && episodeNumber !== null,
  })
}