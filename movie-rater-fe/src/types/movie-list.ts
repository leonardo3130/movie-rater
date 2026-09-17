export interface CreateMovieListRequest {
  name: string
  description: string | null
  isPrivate: boolean
  canGroupEdit: boolean
  groupIds: string[]
}

export type UpdateMovieListRequest = CreateMovieListRequest

export interface MovieListBaseDto {
  id: string
  name: string
  description: string | null
  isPrivate: boolean
  canGroupEdit: boolean
  groupIds: string[]
}

export interface MovieListSummaryDto extends MovieListBaseDto {
  isOwner: boolean
  movieCount: number
  createdAt: string
  lastUpdatedAt: string
}

export interface MovieListItemDto {
  id: string
  tmdbId: number
  title: string
  posterUrl: string | null
  backdropUrl: string | null
  releaseDate: string | null
  voteAverage: number
  addedAt: string
}

export interface MovieListResponseDto extends MovieListBaseDto {
  isOwner: boolean
  createdAt: string
  lastUpdatedAt: string
  movies: MovieListItemDto[]
}
