import client from '../client'
import type {
  UserMovieResponse,
  PagedUserMoviesResponse,
  UserMovieListRequest,
} from '@src/types/user-movie'

export function getUserMovie(movieId: string) {
  return client.get<UserMovieResponse>(`/api/user-movies/${movieId}`).then((r) => r.data)
}

export function setFavorite(movieId: string) {
  return client.post<UserMovieResponse>(`/api/user-movies/${movieId}/favorite`).then((r) => r.data)
}

export function removeFavorite(movieId: string) {
  return client
    .delete<UserMovieResponse>(`/api/user-movies/${movieId}/favorite`)
    .then((r) => r.data)
}

export function setWatchlist(movieId: string) {
  return client
    .post<UserMovieResponse>(`/api/user-movies/${movieId}/watchlist`)
    .then((r) => r.data)
}

export function removeWatchlist(movieId: string) {
  return client
    .delete<UserMovieResponse>(`/api/user-movies/${movieId}/watchlist`)
    .then((r) => r.data)
}

export function getUserMovies(params: UserMovieListRequest = {}) {
  const qp = new URLSearchParams()
  if (params.favoritesOnly) qp.set('favoritesOnly', 'true')
  if (params.watchlistOnly) qp.set('watchlistOnly', 'true')
  if (params.page) qp.set('page', String(params.page))
  if (params.pageSize) qp.set('pageSize', String(params.pageSize))
  const qs = qp.toString()
  return client
    .get<PagedUserMoviesResponse>(`/api/user-movies${qs ? `?${qs}` : ''}`)
    .then((r) => r.data)
}