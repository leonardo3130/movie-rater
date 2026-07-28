export interface GenreDto {
  tmdbId: number
  name: string
}

export interface GenresResponse {
  genres: GenreDto[]
}

export interface MovieSummaryDto {
  tmdbId: number
  title: string
  posterUrl: string | null
  backdropUrl: string | null
  overview: string | null
  releaseDate: string | null
  voteAverage: number
  voteCount: number
  genreIds: number[]
  isFavorite: boolean
  isInWatchlist: boolean
  watchedCount: number
}

export interface PagedMoviesResponse {
  page: number
  totalPages: number
  totalResults: number
  results: MovieSummaryDto[]
}

export interface CastMemberDto {
  id: number
  name: string
  character?: string | null
  profileUrl?: string | null
  order: number
}

export interface CrewMemberDto {
  id: number
  name: string
  department?: string | null
  job?: string | null
  profileUrl?: string | null
}

export interface VideoDto {
  key?: string | null
  site?: string | null
  type?: string | null
  name?: string | null
  official: boolean
}

export interface MovieDetailsResponse {
  tmdbId: number
  title: string
  posterUrl: string | null
  backdropUrl: string | null
  overview: string | null
  releaseDate: string | null
  runtime: number | null
  tagline: string | null
  status: string | null
  imdbId: string | null
  homepage: string | null
  budget: number
  revenue: number
  voteAverage: number
  voteCount: number
  genres: GenreDto[]
  cast: CastMemberDto[]
  crew: CrewMemberDto[]
  videos: VideoDto[]
  isFavorite: boolean
  isInWatchlist: boolean
  watchedCount: number
}

export type SortByOption =
  | 'popularity.desc'
  | 'popularity.asc'
  | 'vote_average.desc'
  | 'vote_average.asc'
  | 'primary_release_date.desc'
  | 'primary_release_date.asc'
  | 'revenue.desc'
  | 'revenue.asc'
  | 'original_title.asc'
  | 'original_title.desc'

export interface DiscoverFilters {
  genreIds: number[]
  primaryReleaseYear: string
  primaryReleaseDateGte: string
  primaryReleaseDateLte: string
  voteAverageGte: number
  sortBy: SortByOption
  includeAdult: boolean
}