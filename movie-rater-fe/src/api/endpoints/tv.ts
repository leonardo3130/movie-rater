import client from '../client'
import type {
  PagedTvShowsResponse,
  TvShowDetailsResponse,
  TvSeasonDetailsResponse,
  TvEpisodeDetailsResponse,
} from '@src/types/tv'
import type { GenresResponse } from '@src/types/movie'

export function searchTvShows(params: {
  query: string
  page?: number
  firstAirDateYear?: string
  year?: string
  includeAdult?: boolean
  language?: string
}) {
  const qp = new URLSearchParams()
  qp.set('query', params.query)
  if (params.page) qp.set('page', String(params.page))
  if (params.firstAirDateYear) qp.set('firstAirDateYear', params.firstAirDateYear)
  if (params.year) qp.set('year', params.year)
  if (params.includeAdult) qp.set('includeAdult', 'true')
  if (params.language) qp.set('language', params.language)
  return client.get<PagedTvShowsResponse>(`/api/tv/search?${qp.toString()}`).then((r) => r.data)
}

export function getTvGenres(language?: string) {
  const qs = language ? `?language=${language}` : ''
  return client.get<GenresResponse>(`/api/tv/genres${qs}`).then((r) => r.data)
}

export function getTvShowDetails(tmdbId: number, language?: string) {
  const qs = language ? `?language=${language}` : ''
  return client.get<TvShowDetailsResponse>(`/api/tv/${tmdbId}${qs}`).then((r) => r.data)
}

export function getTvSeason(tmdbId: number, seasonNumber: number, language?: string) {
  const qs = language ? `?language=${language}` : ''
  return client
    .get<TvSeasonDetailsResponse>(`/api/tv/${tmdbId}/seasons/${seasonNumber}${qs}`)
    .then((r) => r.data)
}

export function getTvEpisode(
  tmdbId: number,
  seasonNumber: number,
  episodeNumber: number,
  language?: string,
) {
  const qs = language ? `?language=${language}` : ''
  return client
    .get<TvEpisodeDetailsResponse>(
      `/api/tv/${tmdbId}/seasons/${seasonNumber}/episodes/${episodeNumber}${qs}`,
    )
    .then((r) => r.data)
}