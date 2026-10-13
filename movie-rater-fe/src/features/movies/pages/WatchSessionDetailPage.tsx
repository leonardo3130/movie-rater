import { useState } from 'react'
import { useParams, Link } from 'react-router'
import { motion } from 'framer-motion'
import { ArrowLeft, Calendar, MapPin, FileText, Star, User, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { MoviePoster } from '../components/MoviePoster'
import { WatchSessionEpisodeLabel } from '../components/WatchSessionEpisodeLabel'
import { Stars } from '../components/Stars'
import { RateMovieDialog } from '../components/RateMovieDialog'
import { EditWatchSessionDialog } from '../components/EditWatchSessionDialog'
import { useWatchSession } from '../hooks/use-watch-session'
import { useAuthStore } from '../../../stores/auth-store'
import type { RatingResponseDto } from '@/src/types/rating'
import { useI18n } from '@src/i18n/use-i18n'

export function WatchSessionDetailPage() {
  const { id } = useParams<{ id: string }>()
  const user = useAuthStore((s) => s.user)
  const { t, formatDate } = useI18n()
  const { data: session, isLoading, isError } = useWatchSession(id)
  const [rateDialogOpen, setRateDialogOpen] = useState(false)
  const [editDialogOpen, setEditDialogOpen] = useState(false)

  if (isLoading) {
    return (
      <div className="p-6 space-y-4">
        <Skeleton className="h-6 w-32" />
        <Skeleton className="h-48 w-full rounded-lg" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-3/4" />
      </div>
    )
  }

  if (isError || !session) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">{t('movies.watchSession.notFound')}</p>
      </div>
    )
  }

  const formattedDate = formatDate(new Date(session.watchedAt), {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })

  const myRating = session.ratings.find((r) => r.userId === user?.id)

  return (
    <div className="p-6 space-y-6">
      <Link
        to="/watch-history"
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="size-4" />
        {t('movies.watchSession.backToHistory')}
      </Link>

      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        className="flex flex-col sm:flex-row gap-6"
      >
        <div className="w-32 shrink-0 mx-auto sm:mx-0">
          <MoviePoster src={session.posterUrl} alt={session.title} />
        </div>

        <div className="flex-1 space-y-4">
          <div>
            <h1 className="text-2xl font-bold">{session.title}</h1>
            <WatchSessionEpisodeLabel
              seriesTitle={session.seriesTitle}
              seasonNumber={session.seasonNumber}
              episodeNumber={session.episodeNumber}
              className="mt-1 text-sm"
            />
            <div className="flex flex-wrap items-center gap-3 mt-2 text-sm text-muted-foreground">
              <span className="flex items-center gap-1">
                <Calendar className="size-3.5" />
                {formattedDate}
                {session.createdByUserId === user?.id && (
                  <Button
                    variant="ghost"
                    size="xs"
                    className="ml-1 text-muted-foreground hover:text-foreground"
                    onClick={() => setEditDialogOpen(true)}
                  >
                    <Pencil className="size-3.5" />
                    {t('common.edit')}
                  </Button>
                )}
              </span>
              {session.location && (
                <span className="flex items-center gap-1">
                  <MapPin className="size-3.5" />
                  {session.location}
                </span>
              )}
              <span className="flex items-center gap-1">
                <User className="size-3.5" />
                {t('movies.watchSession.addedBy', { username: session.createdByUsername })}
              </span>
            </div>
          </div>

          {session.notes && (
            <div className="p-3 rounded-lg bg-muted/50">
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground mb-1">
                <FileText className="size-3" />
                {t('movies.watchSession.notes')}
              </div>
              <p className="text-sm leading-relaxed">{session.notes}</p>
            </div>
          )}
        </div>
      </motion.div>

      <Separator />

      <div className="space-y-6">
        <h2 className="font-heading text-lg font-medium tracking-tight">{t('movies.watchSession.ratings')}</h2>

        {session.ratings.length === 0 && (
          <div className="text-center py-8 space-y-3">
            <Star className="size-12 text-muted-foreground/30 mx-auto" />
            <p className="text-muted-foreground">{t('movies.watchSession.noRatingsYet')}</p>
            <Button onClick={() => setRateDialogOpen(true)}>{t('movies.watchSession.rateThis')}</Button>
          </div>
        )}

        {session.ratings.map((rating) => {
          const isMe = rating.userId === user?.id
          return (
            <motion.div
              key={rating.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="p-4 rounded-lg border border-border/50 bg-card"
            >
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <div className="flex size-8 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
                    {rating.username.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <p className="text-sm font-medium">{rating.username}</p>
                    <p className="text-[10px] text-muted-foreground">
                      {isMe ? t('common.you') : t('common.yourPartner')}
                    </p>
                  </div>
                </div>
                <Badge variant="secondary" className="flex items-center gap-1">
                  <Star className="size-3 fill-yellow-500 text-yellow-500" />
                  {rating.ratingValue}/10
                </Badge>
              </div>

              <div className="ml-10">
                <Stars value={rating.ratingValue} size="sm" readonly />
                {rating.review && (
                  <p className="mt-2 text-sm text-muted-foreground leading-relaxed">
                    &ldquo;{rating.review}&rdquo;
                  </p>
                )}
              </div>

              {isMe && (
                <div className="mt-3 ml-10">
                  <Button
                    variant="ghost"
                    size="xs"
                    onClick={() => setRateDialogOpen(true)}
                  >
                    {t('movies.watchSession.editRating')}
                  </Button>
                </div>
              )}
            </motion.div>
          )
        })}
      </div>

      {!myRating && session.ratings.length > 0 && (
        <div className="text-center">
          <Button onClick={() => setRateDialogOpen(true)}>
            <Star className="size-4" />
            {t('movies.watchSession.rateThis')}
          </Button>
        </div>
      )}

      <RateMovieDialog
        open={rateDialogOpen}
        onOpenChange={setRateDialogOpen}
        watchSessionId={session.id}
        title={session.title}
        posterUrl={session.posterUrl}
        existingRating={(myRating as RatingResponseDto) ?? null}
      />

      <EditWatchSessionDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        session={session}
      />
    </div>
  )
}
