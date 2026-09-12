import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Calendar, MapPin, FileText, Loader2, Save } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { useUpdateWatchSession } from '../hooks/use-update-watch-session'
import { updateWatchSessionSchema, type UpdateWatchSessionFormValues } from '../schemas/watch-session.schema'
import type { WatchSessionResponseDto } from '@src/types/watch-session'

interface EditWatchSessionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  session: WatchSessionResponseDto
}

export function EditWatchSessionDialog({
  open,
  onOpenChange,
  session,
}: EditWatchSessionDialogProps) {
  const updateSession = useUpdateWatchSession()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<UpdateWatchSessionFormValues>({
    resolver: zodResolver(updateWatchSessionSchema),
    defaultValues: {
      watchedAt: new Date(session.watchedAt).toISOString().slice(0, 10),
      location: session.location ?? '',
      notes: session.notes ?? '',
    },
  })

  const onSubmit = (values: UpdateWatchSessionFormValues) => {
    updateSession.mutate(
      {
        id: session.id,
        data: {
          watchedAt: new Date(values.watchedAt).toISOString(),
          location: values.location || null,
          notes: values.notes || null,
        },
      },
      {
        onSuccess: () => onOpenChange(false),
      },
    )
  }

  const pending = isSubmitting || updateSession.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Watch Session</DialogTitle>
          <DialogDescription>
            Update when and where you watched {session.movieTitle}
          </DialogDescription>
        </DialogHeader>

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
              <Save className="size-4" />
              Save Changes
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}