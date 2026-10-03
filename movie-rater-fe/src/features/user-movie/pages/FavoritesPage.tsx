import { Heart, Loader2 } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useFavorites } from '../hooks/use-favorites'
import { UserMovieCard } from '../components/UserMovieCard'
import { useNavigate } from 'react-router'

export function FavoritesPage() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const { data, isLoading, isError, isFetching } = useFavorites(page, 20)

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">Failed to load favorites</p>
      </div>
    )
  }

  const items = data?.results ?? []

  if (items.length === 0) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Heart className="size-16 text-muted-foreground/30" />
        <h2 className="text-xl font-semibold">No favorites yet</h2>
        <p className="text-muted-foreground">
          Start discovering movies and TV shows and add your favorites!
        </p>
        <Button onClick={() => navigate('/movies')}>Browse movies</Button>
      </div>
    )
  }

  const totalPages = data?.totalPages ?? 1

  return (
    <div className="p-6 space-y-8">
      <div className="flex items-center gap-3">
        <div className="relative flex size-10 shrink-0 items-center justify-center rounded-xl bg-red-500/10">
          <Heart className="size-5 text-red-500" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">Favorites</h1>
          <p className="text-sm text-muted-foreground">
            {data?.totalResults ?? 0} item{data?.totalResults !== 1 ? 's' : ''}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
        {items.map((item, index) => (
          <UserMovieCard
            key={item.id}
            item={item}
            isFavorite
            isInWatchlist={item.isInWatchlist}
            index={index}
          />
        ))}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1 || isFetching}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            Previous
          </Button>
          <div className="flex items-center gap-1.5">
            {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
              const pageNum = page <= 3
                ? i + 1
                : page >= totalPages - 2
                  ? totalPages - 4 + i
                  : page - 2 + i
              if (pageNum < 1 || pageNum > totalPages) return null
              return (
                <Button
                  key={pageNum}
                  variant={pageNum === page ? 'default' : 'ghost'}
                  size="xs"
                  onClick={() => setPage(pageNum)}
                  className="min-w-[2rem]"
                >
                  {pageNum}
                </Button>
              )
            })}
          </div>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages || isFetching}
            onClick={() => setPage((p) => p + 1)}
          >
            {isFetching ? <Loader2 className="size-3 animate-spin mr-1" /> : null}
            Next
          </Button>
        </div>
      )}
    </div>
  )
}