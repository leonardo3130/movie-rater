export interface GenreStatDto {
  genreName: string
  count: number
  averageRating: number
}

export interface MovieStatDto {
  movieId: string
  title: string
  averageRating: number
  watchedCount: number
}

export interface DashboardResponseDto {
  groupId: string,
  moviesWatched: number
  moviesThisMonth: number
  moviesThisYear: number
  averageRating: number
  favoriteGenres: GenreStatDto[]
  mostWatchedGenres: GenreStatDto[]
  highestRatedMovie: MovieStatDto | null
  lowestRatedMovie: MovieStatDto | null
  biggestDisagreement: MovieStatDto | null
  averageDisagreement: number
  rewatchCount: number
  currentStreak: number
  longestStreak: number
}
