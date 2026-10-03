import type { GenreDto } from '@src/types/movie'
import type { MediaType } from '@src/types/watch-session'

export interface TvShowSummaryDto {
  id: string
  tmdbId: number
  mediaType: MediaType
  title: string
  posterUrl: string | null
  backdropUrl: string | null
  overview: string | null
  firstAirDate: string | null
  voteAverage: number
  voteCount: number
  genreIds: number[]
  isFavorite: boolean
  isInWatchlist: boolean
  watchedCount: number
}

export interface PagedTvShowsResponse {
  page: number
  totalPages: number
  totalResults: number
  results: TvShowSummaryDto[]
}

export interface TvSeasonSummaryDto {
  tmdbId: number
  seasonNumber: number
  episodeCount: number
  overview: string | null
  posterUrl: string | null
  airDate: string | null
}

export interface TvShowDetailsResponse {
  id: string
  tmdbId: number
  name: string
  overview: string | null
  posterUrl: string | null
  backdropUrl: string | null
  firstAirDate: string | null
  lastAirDate: string | null
  numberOfSeasons: number
  numberOfEpisodes: number
  status: string | null
  type: string | null
  voteAverage: number
  voteCount: number
  genres: GenreDto[]
  seasons: TvSeasonSummaryDto[]
  isFavorite: boolean
  isInWatchlist: boolean
  watchedCount: number
}

export interface TvEpisodeSummaryDto {
  tmdbId: number
  episodeNumber: number
  name: string
  overview: string | null
  stillUrl: string | null
  airDate: string | null
  runtime: number | null
  voteAverage: number
}

export interface TvSeasonDetailsResponse {
  id: string
  tmdbId: number
  name: string
  seriesTmdbId: number
  seasonNumber: number
  overview: string | null
  posterUrl: string | null
  airDate: string | null
  voteAverage: number
  voteCount: number
  episodes: TvEpisodeSummaryDto[]
  isFavorite: boolean
  isInWatchlist: boolean
  watchedCount: number
}

export interface TvEpisodeDetailsResponse {
  id: string
  tmdbId: number
  name: string
  seriesTmdbId: number
  seriesTitle: string | null
  seasonNumber: number
  episodeNumber: number
  overview: string | null
  stillUrl: string | null
  airDate: string | null
  runtime: number | null
  voteAverage: number
  isFavorite: boolean
  isInWatchlist: boolean
  watchedCount: number
}