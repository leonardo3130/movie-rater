import { motion } from 'framer-motion'
import {
  LayoutDashboard,
  Clapperboard,
  Calendar,
  TrendingUp,
  Star,
  Repeat,
  Flame,
  Award,
  Zap,
  ThumbsUp,
  ThumbsDown,
  Film,
  Gauge,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { useDashboard } from '../hooks/use-dashboard'
import type { GenreStatDto, MovieStatDto } from '@src/types/dashboard'

const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: { staggerChildren: 0.06 },
  },
} as const

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0, transition: { duration: 0.4, ease: 'easeOut' as const } },
} as const

function StatCardSkeleton() {
  return (
    <Card className="overflow-hidden border-border/50 bg-card/50">
      <CardContent className="p-5">
        <Skeleton className="h-4 w-24 mb-3" />
        <Skeleton className="h-9 w-16 mb-2" />
        <Skeleton className="h-3 w-20" />
      </CardContent>
    </Card>
  )
}

function StatCard({
  icon: Icon,
  label,
  value,
  sublabel,
  gradient,
  iconBg,
  iconColor,
}: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  value: string | number
  sublabel?: string
  gradient: string
  iconBg: string
  iconColor: string
}) {
  return (
    <motion.div variants={item} whileHover={{ y: -4, transition: { duration: 0.2 } }}>
      <Card className="group relative overflow-hidden border-border/50 bg-card/50 backdrop-blur-sm transition-shadow hover:shadow-lg hover:shadow-primary/5">
        <div className={`absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-500 ${gradient}`} />
        <CardContent className="relative p-5">
          <div className="flex items-start justify-between">
            <div className="space-y-1.5">
              <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
                {label}
              </p>
              <p className="text-3xl font-bold tabular-nums tracking-tight">
                {value}
              </p>
              {sublabel && (
                <p className="text-xs text-muted-foreground">{sublabel}</p>
              )}
            </div>
            <div className={`flex size-10 shrink-0 items-center justify-center rounded-xl ${iconBg}`}>
              <Icon className={`size-5 ${iconColor}`} />
            </div>
          </div>
        </CardContent>
      </Card>
    </motion.div>
  )
}

function GenreSection({
  title,
  icon: Icon,
  genres,
  colorClass,
  emptyLabel,
}: {
  title: string
  icon: React.ComponentType<{ className?: string }>
  genres: GenreStatDto[]
  colorClass: string
  emptyLabel: string
}) {
  const maxCount = genres.length > 0 ? Math.max(...genres.map((g) => g.count)) : 1

  return (
    <motion.div variants={item}>
      <Card className="h-full border-border/50 bg-card/50">
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <Icon className={`size-4 ${colorClass}`} />
            {title}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {genres.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4 text-center">{emptyLabel}</p>
          ) : (
            genres.map((genre) => (
              <div key={genre.genreName} className="space-y-1.5">
                <div className="flex items-center justify-between text-sm">
                  <span className="font-medium truncate">{genre.genreName}</span>
                  <span className="ml-2 shrink-0 text-xs text-muted-foreground tabular-nums">
                    {genre.count} {genre.count === 1 ? 'film' : 'films'}
                    {genre.averageRating > 0 && (
                      <span className="ml-1.5">
                        <span className="text-yellow-500">★</span> {genre.averageRating.toFixed(1)}
                      </span>
                    )}
                  </span>
                </div>
                <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                  <motion.div
                    className={`h-full rounded-full ${colorClass}`}
                    initial={{ width: 0 }}
                    animate={{ width: `${(genre.count / maxCount) * 100}%` }}
                    transition={{ duration: 1, delay: 0.3, ease: 'easeOut' as const }}
                  />
                </div>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </motion.div>
  )
}

function MovieHighlightCard({
  icon: Icon,
  label,
  movie,
  colorClass,
  iconBg,
  emptyLabel,
}: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  movie: MovieStatDto | null
  colorClass: string
  iconBg: string
  emptyLabel: string
}) {
  return (
    <motion.div variants={item} whileHover={{ y: -4, transition: { duration: 0.2 } }}>
      <Card className="h-full border-border/50 bg-card/50 transition-shadow hover:shadow-lg hover:shadow-primary/5">
        <CardHeader className="pb-2">
          <CardTitle className="flex items-center gap-2 text-sm">
            <div className={`flex size-8 shrink-0 items-center justify-center rounded-lg ${iconBg}`}>
              <Icon className={`size-4 ${colorClass}`} />
            </div>
            {label}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {movie ? (
            <div className="space-y-2">
              <p className="font-semibold leading-tight line-clamp-2">{movie.title}</p>
              <div className="flex items-center gap-2">
                <div className="flex items-center gap-1">
                  <Star className="size-3.5 fill-yellow-500 text-yellow-500" />
                  <span className="text-sm font-medium tabular-nums">
                    {movie.averageRating.toFixed(1)}
                  </span>
                </div>
                {movie.watchedCount > 1 && (
                  <Badge variant="secondary" className="text-[0.65rem]">
                    {movie.watchedCount}x
                  </Badge>
                )}
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground py-2">{emptyLabel}</p>
          )}
        </CardContent>
      </Card>
    </motion.div>
  )
}

function RatingBar({ value, max = 10 }: { value: number; max?: number }) {
  const pct = (value / max) * 100
  const color =
    value >= 8
      ? 'bg-emerald-500'
      : value >= 7
        ? 'bg-chart-3'
        : value >= 5
          ? 'bg-yellow-500'
          : value >= 4
            ? 'bg-orange-500'
            : 'bg-red-500'

  return (
    <div className="flex items-center gap-2">
      <div className="h-2 flex-1 overflow-hidden rounded-full bg-muted">
        <motion.div
          className={`h-full rounded-full ${color}`}
          initial={{ width: 0 }}
          animate={{ width: `${pct}%` }}
          transition={{ duration: 1, delay: 0.5, ease: 'easeOut' as const }}
        />
      </div>
      <span className="text-xs font-medium tabular-nums text-muted-foreground w-5 text-right">
        {value.toFixed(1)}
      </span>
    </div>
  )
}

export function DashboardPage() {
  const { data, isLoading, isError } = useDashboard()

  if (isLoading) {
    return (
      <div className="p-6 space-y-8">
        <div className="flex items-center gap-3">
          <div className="relative flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10">
            <LayoutDashboard className="size-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold">Dashboard</h1>
            <Skeleton className="h-4 w-32 mt-1" />
          </div>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <StatCardSkeleton key={i} />
          ))}
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <StatCardSkeleton />
          <StatCardSkeleton />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <Card className="border-border/50 bg-card/50">
            <CardHeader>
              <Skeleton className="h-5 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-6 w-full" />
              ))}
            </CardContent>
          </Card>
          <Card className="border-border/50 bg-card/50">
            <CardHeader>
              <Skeleton className="h-5 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-6 w-full" />
              ))}
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  if (isError || !data) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">Failed to load dashboard</p>
      </div>
    )
  }

  const hasData = data.moviesWatched > 0

  return (
    <div className="p-6 space-y-8">
      <motion.div
        initial={{ opacity: 0, y: -10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="flex items-center gap-3"
      >
        <div className="relative flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10">
          <LayoutDashboard className="size-5 text-primary" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Dashboard</h1>
          <p className="text-sm text-muted-foreground">
            {hasData
              ? `You've watched ${data.moviesWatched} movie${data.moviesWatched !== 1 ? 's' : ''} together`
              : 'Start watching movies together!'}
          </p>
        </div>
      </motion.div>

      {!hasData ? (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.2 }}
          className="flex min-h-[40vh] flex-col items-center justify-center gap-4"
        >
          <Clapperboard className="size-20 text-muted-foreground/20" />
          <h2 className="text-xl font-semibold text-muted-foreground">No movies yet</h2>
          <p className="text-sm text-muted-foreground max-w-sm text-center">
            Mark your first movie as watched to unlock your dashboard statistics.
          </p>
        </motion.div>
      ) : (
        <motion.div
          variants={container}
          initial="hidden"
          animate="show"
          className="space-y-8"
        >
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4">
            <StatCard
              icon={Clapperboard}
              label="Movies Watched"
              value={data.moviesWatched}
              iconBg="bg-blue-500/10"
              iconColor="text-blue-400"
              gradient="bg-gradient-to-br from-blue-500/5 to-transparent"
            />
            <StatCard
              icon={Calendar}
              label="This Month"
              value={data.moviesThisMonth}
              sublabel={data.moviesThisMonth === 1 ? 'movie this month' : 'movies this month'}
              iconBg="bg-emerald-500/10"
              iconColor="text-emerald-400"
              gradient="bg-gradient-to-br from-emerald-500/5 to-transparent"
            />
            <StatCard
              icon={TrendingUp}
              label="This Year"
              value={data.moviesThisYear}
              sublabel={data.moviesThisYear === 1 ? 'movie this year' : 'movies this year'}
              iconBg="bg-violet-500/10"
              iconColor="text-violet-400"
              gradient="bg-gradient-to-br from-violet-500/5 to-transparent"
            />
            <StatCard
              icon={Star}
              label="Average Rating"
              value={data.averageRating.toFixed(1)}
              sublabel="out of 10"
              iconBg="bg-yellow-500/10"
              iconColor="text-yellow-400"
              gradient="bg-gradient-to-br from-yellow-500/5 to-transparent"
            />
            <StatCard
              icon={Repeat}
              label="Rewatches"
              value={data.rewatchCount}
              sublabel={data.rewatchCount === 1 ? 'rewatch' : 'rewatches'}
              iconBg="bg-rose-500/10"
              iconColor="text-rose-400"
              gradient="bg-gradient-to-br from-rose-500/5 to-transparent"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <StatCard
              icon={Flame}
              label="Current Streak"
              value={`${data.currentStreak} week${data.currentStreak !== 1 ? 's' : ''}`}
              sublabel="consecutive weeks with a movie"
              iconBg="bg-orange-500/10"
              iconColor="text-orange-400"
              gradient="bg-gradient-to-br from-orange-500/5 to-transparent"
            />
            <StatCard
              icon={Award}
              label="Longest Streak"
              value={`${data.longestStreak} week${data.longestStreak !== 1 ? 's' : ''}`}
              sublabel="all-time best streak"
              iconBg="bg-amber-500/10"
              iconColor="text-amber-400"
              gradient="bg-gradient-to-br from-amber-500/5 to-transparent"
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <GenreSection
              title="Favorite Genres"
              icon={ThumbsUp}
              genres={data.favoriteGenres}
              colorClass="bg-gradient-to-r from-chart-1 to-chart-2"
              emptyLabel="Rate more movies to discover your favorite genres"
            />
            <GenreSection
              title="Most Watched Genres"
              icon={Film}
              genres={data.mostWatchedGenres}
              colorClass="bg-gradient-to-r from-chart-3 to-chart-4"
              emptyLabel="Watch more movies to see your top genres"
            />
          </div>

          <div>
            <motion.div variants={item} className="mb-4">
              <h2 className="text-lg font-semibold flex items-center gap-2">
                <Zap className="size-4 text-yellow-400" />
                Movie Highlights
              </h2>
            </motion.div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <MovieHighlightCard
                icon={ThumbsUp}
                label="Highest Rated"
                movie={data.highestRatedMovie}
                colorClass="text-emerald-400"
                iconBg="bg-emerald-500/10"
                emptyLabel="No ratings yet"
              />
              <MovieHighlightCard
                icon={ThumbsDown}
                label="Lowest Rated"
                movie={data.lowestRatedMovie}
                colorClass="text-red-400"
                iconBg="bg-red-500/10"
                emptyLabel="No ratings yet"
              />
              <MovieHighlightCard
                icon={Zap}
                label="Biggest Disagreement"
                movie={data.biggestDisagreement}
                colorClass="text-orange-400"
                iconBg="bg-orange-500/10"
                emptyLabel="Rate the same movies to compare"
              />
            </div>
          </div>

          <motion.div variants={item}>
            <Card className="border-border/50 bg-card/50">
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-base">
                  <Gauge className="size-4 text-chart-3" />
                  Rating Compatibility
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium">Average Disagreement</p>
                    <p className="text-xs text-muted-foreground">
                      How far apart your ratings typically are
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-2xl font-bold tabular-nums">
                      {data.averageDisagreement.toFixed(1)}
                    </p>
                    <p className="text-xs text-muted-foreground">points apart</p>
                  </div>
                </div>
                <Separator />
                <div className="space-y-2">
                  <RatingBar value={10 - data.averageDisagreement} max={10} />
                  <div className="flex justify-between text-xs text-muted-foreground">
                    <span>Perfect match (10)</span>
                    <span>Compatibility Score</span>
                    <span>Polar opposites (0)</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </motion.div>
      )}
    </div>
  )
}