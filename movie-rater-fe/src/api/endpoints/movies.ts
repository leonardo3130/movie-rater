import client from '../client'
import type {
  PagedMoviesResponse,
  MovieDetailsResponse,
  GenresResponse,
  SortByOption,
} from '@src/types/movie'

interface PageParam {
  page?: number
  language?: string
  region?: string
}

export function searchMovies(params: {
  query: string
  page?: number
  year?: string
  primaryReleaseYear?: string
  includeAdult?: boolean
  region?: string
  language?: string
}) {
  const qp = new URLSearchParams()
  qp.set('query', params.query)
  if (params.page) qp.set('page', String(params.page))
  if (params.year) qp.set('year', params.year)
  if (params.primaryReleaseYear) qp.set('primaryReleaseYear', params.primaryReleaseYear)
  if (params.includeAdult) qp.set('includeAdult', 'true')
  if (params.region) qp.set('region', params.region)
  if (params.language) qp.set('language', params.language)
  return client.get<PagedMoviesResponse>(`/api/movies/search?${qp.toString()}`).then((r) => r.data)
}

export function discoverMovies(params: {
  page?: number
  genreIds?: string
  primaryReleaseYear?: string
  primaryReleaseDateGte?: string
  primaryReleaseDateLte?: string
  sortBy?: SortByOption
  voteAverageGte?: number
  includeAdult?: boolean
  region?: string
  language?: string
}) {
  const qp = new URLSearchParams()
  if (params.page) qp.set('page', String(params.page))
  if (params.genreIds) qp.set('genreIds', params.genreIds)
  if (params.primaryReleaseYear) qp.set('primaryReleaseYear', params.primaryReleaseYear)
  if (params.primaryReleaseDateGte) qp.set('primaryReleaseDateGte', params.primaryReleaseDateGte)
  if (params.primaryReleaseDateLte) qp.set('primaryReleaseDateLte', params.primaryReleaseDateLte)
  if (params.sortBy) qp.set('sortBy', params.sortBy)
  if (params.voteAverageGte) qp.set('voteAverageGte', String(params.voteAverageGte))
  if (params.includeAdult) qp.set('includeAdult', 'true')
  if (params.region) qp.set('region', params.region)
  if (params.language) qp.set('language', params.language)
  return client.get<PagedMoviesResponse>(`/api/movies/discover?${qp.toString()}`).then((r) => r.data)
}

export function getPopularMovies(params: PageParam = {}) {
  const qp = new URLSearchParams()
  if (params.page) qp.set('page', String(params.page))
  if (params.language) qp.set('language', params.language)
  if (params.region) qp.set('region', params.region)
  return client.get<PagedMoviesResponse>(`/api/movies/popular?${qp.toString()}`).then((r) => r.data)
}

export function getNowPlayingMovies(params: PageParam = {}) {
  const qp = new URLSearchParams()
  if (params.page) qp.set('page', String(params.page))
  if (params.language) qp.set('language', params.language)
  if (params.region) qp.set('region', params.region)
  return client.get<PagedMoviesResponse>(`/api/movies/now-playing?${qp.toString()}`).then((r) => r.data)
}

export function getTopRatedMovies(params: PageParam = {}) {
  const qp = new URLSearchParams()
  if (params.page) qp.set('page', String(params.page))
  if (params.language) qp.set('language', params.language)
  if (params.region) qp.set('region', params.region)
  return client.get<PagedMoviesResponse>(`/api/movies/top-rated?${qp.toString()}`).then((r) => r.data)
}

export function getMovieDetails(tmdbId: number, language?: string) {
  const qs = language ? `?language=${language}` : ''
  return client.get<MovieDetailsResponse>(`/api/movies/${tmdbId}${qs}`).then((r) => r.data)
}

export function getMovieRecommendations(tmdbId: number, params: PageParam = {}) {
  const qp = new URLSearchParams()
  if (params.page) qp.set('page', String(params.page))
  if (params.language) qp.set('language', params.language)
  return client.get<PagedMoviesResponse>(`/api/movies/${tmdbId}/recommendations?${qp.toString()}`).then((r) => r.data)
}

export function getGenres(language?: string) {
  const qs = language ? `?language=${language}` : ''
  return client.get<GenresResponse>(`/api/movies/genres${qs}`).then((r) => r.data)
}
