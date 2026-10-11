import { useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router'
import { motion } from 'framer-motion'
import { Library, Loader2, Pencil, Trash2, Star, Lock, Users, X, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { useMovieList } from '../hooks/use-movie-list'
import { useGroups } from '../../groups/hooks/use-groups'
import { EditListDialog } from '../components/ListFormDialog'
import { DeleteListDialog } from '../components/DeleteListDialog'
import { RemoveMovieDialog } from '../components/RemoveMovieDialog'
import type { MovieListItemDto } from '@src/types/movie-list'
import { useI18n } from '@src/i18n/use-i18n'

export function MovieListDetailPage() {
  const { listId } = useParams<{ listId: string }>()
  const navigate = useNavigate()
  const groups = useGroups()
  const { t } = useI18n()

  const { data: list, isLoading, isError, refetch } = useMovieList(listId)

  const [editOpen, setEditOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [removingMovie, setRemovingMovie] = useState<MovieListItemDto | null>(null)

  const userGroupIds = new Set((groups.data ?? []).map((g) => g.id))
  const canEdit = list
    ? list.isOwner || (list.canGroupEdit && list.groupIds.some((id) => userGroupIds.has(id)))
    : false

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError || !list) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Library className="size-16 text-muted-foreground/30" />
        <h2 className="text-xl font-semibold">{t('lists.notFound')}</h2>
        <p className="text-muted-foreground">{t('lists.notFoundDescription')}</p>
        <Button variant="outline" onClick={() => navigate('/lists')}>
          {t('lists.backToLists')}
        </Button>
        <Button variant="ghost" onClick={() => refetch()}>
          {t('common.tryAgain')}
        </Button>
      </div>
    )
  }

  const year = (movie: MovieListItemDto) =>
    movie.releaseDate ? movie.releaseDate.slice(0, 4) : null

  return (
    <div className="p-6 space-y-6">
      <div className="flex flex-wrap items-center gap-2">
        <Button variant="ghost" size="sm" onClick={() => navigate('/lists')}>
          <ArrowLeft className="size-4" />
          {t('lists.backToLists')}
        </Button>
      </div>

      <div className="flex flex-wrap items-start gap-3 justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="font-heading text-2xl font-bold truncate max-w-full">{list.name}</h1>
            {list.isPrivate ? (
              <Badge variant="outline" className="flex items-center gap-1 text-[10px]">
                <Lock className="size-2.5" />
                {t('lists.private')}
              </Badge>
            ) : (
              <Badge variant="outline" className="flex items-center gap-1 text-[10px]">
                <Users className="size-2.5" />
                {t('lists.shared')}
              </Badge>
            )}
          </div>
          {list.description && (
            <p className="text-sm text-muted-foreground mt-1">{list.description}</p>
          )}
          <p className="text-xs text-muted-foreground mt-1">
            {t('lists.movieCount', { count: list.movies.length })}
          </p>
        </div>
        {canEdit && (
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setEditOpen(true)}>
              <Pencil className="size-4" />
              {t('common.edit')}
            </Button>
            {list.isOwner && (
              <Button variant="destructive" size="sm" onClick={() => setDeleteOpen(true)}>
                <Trash2 className="size-4" />
                {t('common.delete')}
              </Button>
            )}
          </div>
        )}
      </div>

      {list.movies.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <Library className="size-16 text-muted-foreground/30" />
          <h2 className="text-lg font-semibold">{t('lists.empty')}</h2>
          <p className="text-muted-foreground">
            {t('lists.emptyDescription')}
          </p>
          <Button onClick={() => navigate('/movies')}>{t('lists.browseMovies')}</Button>
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
          {list.movies.map((movie, index) => {
            const movieYear = year(movie)
            return (
              <motion.div
                key={movie.id}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3, delay: index * 0.03, ease: 'easeOut' }}
                whileHover={{ y: -4 }}
                className="group shrink-0"
              >
                <Link to={`/movies/${movie.tmdbId}`} className="block space-y-2">
                  <div className="relative overflow-hidden rounded-lg">
                    <MoviePoster src={movie.posterUrl} alt={movie.title} />
                    <div className="absolute top-2 right-2">
                      <Badge variant="secondary" className="flex items-center gap-1 text-xs">
                        <Star className="size-3 fill-yellow-500 text-yellow-500" />
                        {movie.voteAverage.toFixed(1)}
                      </Badge>
                    </div>
                    {canEdit && (
                      <div className="absolute bottom-2 left-2 opacity-0 group-hover:opacity-100 transition-opacity focus-within:opacity-100">
                        <motion.button
                          type="button"
                          whileTap={{ scale: 0.8 }}
                          onClick={(e) => {
                            e.preventDefault()
                            e.stopPropagation()
                            setRemovingMovie(movie)
                          }}
                          className="rounded-full p-1 cursor-pointer transition-colors hover:bg-red-500/20 text-white/70 hover:text-red-400"
                          aria-label={t('lists.removeFromListAria', { title: movie.title })}
                        >
                          <X className="size-3.5" />
                        </motion.button>
                      </div>
                    )}
                  </div>
                  <div className="space-y-0.5">
                    <p className="text-sm font-medium leading-tight text-foreground line-clamp-2 group-hover:text-primary transition-colors">
                      {movie.title}
                    </p>
                    {movieYear && <p className="text-xs text-muted-foreground">{movieYear}</p>}
                  </div>
                </Link>
              </motion.div>
            )
          })}
        </div>
      )}

      <EditListDialog open={editOpen} onOpenChange={(open) => { if (!open) setEditOpen(false) }} list={list} />
      <DeleteListDialog
        open={deleteOpen}
        onOpenChange={(open) => { if (!open) setDeleteOpen(false) }}
        list={list}
        onSuccess={() => navigate('/lists')}
      />
      <RemoveMovieDialog
        open={!!removingMovie}
        onOpenChange={(open) => { if (!open) setRemovingMovie(null) }}
        listId={list.id}
        movie={removingMovie}
      />
    </div>
  )
}