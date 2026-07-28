export interface UserMovieResponse {
  userId: string
  movieId: string
  isFavorite: boolean
  isInWatchlist: boolean
  createdAt: string
  updatedAt: string
}

export interface UserMovieWithMovie {
  id: string
  tmdbId: number
  title: string
  posterUrl: string | null
  backdropUrl: string | null
  releaseDate: string | null
  voteAverage: number
  isFavorite: boolean
  isInWatchlist: boolean
  createdAt: string
  updatedAt: string
}

export interface PagedUserMoviesResponse {
  page: number
  totalPages: number
  totalResults: number
  results: UserMovieWithMovie[]
}

export interface UserMovieListRequest {
  favoritesOnly?: boolean
  watchlistOnly?: boolean
  page?: number
  pageSize?: number
}