import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Calendar, MapPin, FileText, Loader2, Eye } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { MoviePoster } from './MoviePoster'
import { useCreateWatchSession } from '../hooks/use-create-watch-session'
import { createWatchSessionSchema, type CreateWatchSessionFormValues } from '../schemas/watch-session.schema'

interface CreateWatchSessionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  movieId: string
  movieTitle: string
  moviePosterUrl: string | null
  onSuccess?: (sessionId: string) => void
}

export function CreateWatchSessionDialog({
  open,
  onOpenChange,
  movieId,
  movieTitle,
  moviePosterUrl,
  onSuccess,
}: CreateWatchSessionDialogProps) {
  const createSession = useCreateWatchSession()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<CreateWatchSessionFormValues>({
    resolver: zodResolver(createWatchSessionSchema),
    defaultValues: {
      watchedAt: new Date().toISOString().slice(0, 10),
      location: '',
      notes: '',
    },
  })

  const onSubmit = (values: CreateWatchSessionFormValues) => {
    createSession.mutate(
      {
        movieId,
        watchedAt: new Date(values.watchedAt).toISOString(),
        location: values.location || null,
        notes: values.notes || null,
      },
      {
        onSuccess: (data) => {
          onOpenChange(false)
          if (onSuccess) onSuccess(data.id)
        },
      },
    )
  }

  const pending = isSubmitting || createSession.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Mark as Watched</DialogTitle>
          <DialogDescription>
            Record when you watched this movie together
          </DialogDescription>
        </DialogHeader>

        <div className="flex items-center gap-3 mb-4 p-3 rounded-lg bg-muted/50">
          <div className="w-12 shrink-0">
            <MoviePoster src={moviePosterUrl} alt={movieTitle} />
          </div>
          <div className="min-w-0">
            <p className="font-medium text-sm leading-tight line-clamp-2">{movieTitle}</p>
          </div>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="watchedAt" className="flex items-center gap-1.5">
              <Calendar className="size-3.5 text-muted-foreground" />
              When did you watch it?
            </Label>
            <Input
              id="watchedAt"
              type="date"
              aria-invalid={!!errors.watchedAt}
              {...register('watchedAt')}
            />
            {errors.watchedAt && (
              <p className="text-xs text-destructive">{errors.watchedAt.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="location" className="flex items-center gap-1.5">
              <MapPin className="size-3.5 text-muted-foreground" />
              Location (optional)
            </Label>
            <Input
              id="location"
              placeholder="e.g. Home, Cinema, ..."
              aria-invalid={!!errors.location}
              {...register('location')}
            />
            {errors.location && (
              <p className="text-xs text-destructive">{errors.location.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="notes" className="flex items-center gap-1.5">
              <FileText className="size-3.5 text-muted-foreground" />
              Notes (optional)
            </Label>
            <textarea
              id="notes"
              rows={3}
              placeholder="How was the movie night?"
              className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 resize-none"
              aria-invalid={!!errors.notes}
              {...register('notes')}
            />
            {errors.notes && (
              <p className="text-xs text-destructive">{errors.notes.message}</p>
            )}
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={pending}
            >
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={pending}>
              {pending && <Loader2 className="size-4 animate-spin" />}
              <Eye className="size-4" />
              Mark as Watched
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}