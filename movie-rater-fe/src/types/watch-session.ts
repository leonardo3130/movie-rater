export interface RatingSummaryDto {
  id: string
  userId: string
  username: string
  ratingValue: number
  review: string | null
}

export interface WatchSessionResponseDto {
  id: string
  movieId: string
  movieTitle: string
  moviePosterUrl: string | null
  watchedAt: string
  location: string | null
  notes: string | null
  createdByUserId: string
  createdByUsername: string
  createdAt: string
  ratings: RatingSummaryDto[]
}

export interface WatchSessionListItemDto {
  id: string
  movieId: string
  movieTitle: string
  moviePosterUrl: string | null
  watchedAt: string
  location: string | null
  notes: string | null
  createdByUserId: string
  createdByUsername: string
  createdAt: string
  ratingCount: number
}

export interface WatchSessionListResponseDto {
  items: WatchSessionListItemDto[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CreateWatchSessionRequestDto {
  movieId: string
  watchedAt: string
  location?: string | null
  notes?: string | null
}

export interface WatchSessionQueryDto {
  movieId?: string
  page?: number
  pageSize?: number
}

export interface HeatmapResponseDto {
  dailyCounts: Record<string, number>
}