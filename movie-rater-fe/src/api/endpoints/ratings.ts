import client from '../client'
import type {
  RatingResponseDto,
  SessionRatingsResponseDto,
  CreateRatingRequestDto,
  UpdateRatingRequestDto,
} from '@src/types/rating'

export function createRating(watchSessionId: string, data: CreateRatingRequestDto) {
  return client
    .post<RatingResponseDto>(`/api/watch-sessions/${watchSessionId}/ratings`, data)
    .then((r) => r.data)
}

export function updateRating(watchSessionId: string, data: UpdateRatingRequestDto) {
  return client
    .put<RatingResponseDto>(`/api/watch-sessions/${watchSessionId}/ratings`, data)
    .then((r) => r.data)
}

export function getSessionRatings(watchSessionId: string) {
  return client
    .get<SessionRatingsResponseDto>(`/api/watch-sessions/${watchSessionId}/ratings`)
    .then((r) => r.data)
}