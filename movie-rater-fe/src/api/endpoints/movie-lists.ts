import client from '../client'
import type {
  CreateMovieListRequest,
  MovieListResponseDto,
  MovieListSummaryDto,
  UpdateMovieListRequest,
} from '@src/types/movie-list'

export function getMovieLists() {
  return client.get<MovieListSummaryDto[]>('/api/movie-lists').then((r) => r.data)
}

export function getMovieList(listId: string) {
  return client.get<MovieListResponseDto>(`/api/movie-lists/${listId}`).then((r) => r.data)
}

export function createMovieList(body: CreateMovieListRequest) {
  return client.post<MovieListResponseDto>('/api/movie-lists', body).then((r) => r.data)
}

export function updateMovieList(listId: string, body: UpdateMovieListRequest) {
  return client.patch<MovieListResponseDto>(`/api/movie-lists/${listId}`, body).then((r) => r.data)
}

export function deleteMovieList(listId: string) {
  return client.delete(`/api/movie-lists/${listId}`).then((r) => r.data)
}

export function addMovieToList(listId: string, movieId: string) {
  return client.post(`/api/movie-lists/${listId}/movies/${movieId}`).then((r) => r.data)
}

export function removeMovieFromList(listId: string, movieId: string) {
  return client.delete(`/api/movie-lists/${listId}/movies/${movieId}`).then((r) => r.data)
}