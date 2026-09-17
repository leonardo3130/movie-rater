import { Loader2, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useRemoveMovieFromList } from '../hooks/use-remove-movie-from-list'
import type { MovieListItemDto } from '@src/types/movie-list'

interface RemoveMovieDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  listId: string
  movie: MovieListItemDto | null
  onSuccess?: () => void
}

export function RemoveMovieDialog({ open, onOpenChange, listId, movie, onSuccess }: RemoveMovieDialogProps) {
  const removeMovie = useRemoveMovieFromList()

  const handleRemove = () => {
    if (!movie) return
    removeMovie.mutate(
      { listId, movieId: movie.id },
      {
        onSuccess: () => {
          onOpenChange(false)
          onSuccess?.()
        },
      },
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md max-h-[40vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Remove movie?</DialogTitle>
          <DialogDescription>
            &quot;{movie?.title ?? 'This movie'}&quot; will be removed from this list.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)} disabled={removeMovie.isPending}>
            Cancel
          </Button>
          <Button variant="destructive" size="sm" onClick={handleRemove} disabled={removeMovie.isPending}>
            {removeMovie.isPending && <Loader2 className="size-4 animate-spin" />}
            <Trash2 className="size-4" />
            Remove
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}