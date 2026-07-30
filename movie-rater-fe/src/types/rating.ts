export interface RatingResponseDto {
  id: string
  userId: string
  username: string
  ratingValue: number
  review: string | null
  createdAt: string
  updatedAt: string
}

export interface SessionRatingsResponseDto {
  watchSessionId: string
  ratings: RatingResponseDto[]
}

export interface CreateRatingRequestDto {
  ratingValue: number
  review?: string | null
}

export interface UpdateRatingRequestDto {
  ratingValue: number
  review?: string | null
}