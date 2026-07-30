import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { MoviePoster } from './MoviePoster'
import { Stars } from './Stars'
import { useCreateRating } from '../hooks/use-create-rating'
import { useUpdateRating } from '../hooks/use-update-rating'
import { useAuthStore } from '../../../stores/auth-store'
import type { RatingResponseDto } from '@src/types/rating'

interface RateMovieDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  watchSessionId: string
  movieTitle: string
  moviePosterUrl: string | null
  existingRating?: RatingResponseDto | null
}

export function RateMovieDialog({
  open,
  onOpenChange,
  watchSessionId,
  movieTitle,
  moviePosterUrl,
  existingRating,
}: RateMovieDialogProps) {
  const user = useAuthStore((s) => s.user)
  const createRating = useCreateRating()
  const updateRating = useUpdateRating()
  const [ratingValue, setRatingValue] = useState(existingRating?.ratingValue ?? 0)
  const [review, setReview] = useState(existingRating?.review ?? '')

  const isEditing = !!existingRating
  const isOwnRating = existingRating?.userId === user?.id

  const handleSubmit = () => {
    if (ratingValue < 1) return

    const data = { watchSessionId, ratingValue, review: review || null }

    if (isEditing && isOwnRating) {
      updateRating.mutate(data, {
        onSuccess: () => onOpenChange(false),
      })
    } else {
      createRating.mutate(data, {
        onSuccess: () => onOpenChange(false),
      })
    }
  }

  const pending = createRating.isPending || updateRating.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Your Rating' : 'Rate the Movie'}</DialogTitle>
          <DialogDescription>
            {isEditing ? 'Update your rating and review' : 'Share your thoughts on this movie'}
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

        <div className="space-y-6">
          <div className="space-y-3">
            <p className="text-sm font-medium text-center">
              Your Rating: <span className="text-yellow-500 font-bold">{ratingValue}/10</span>
            </p>
            <div className="flex justify-center">
              <Stars value={ratingValue} onChange={setRatingValue} size="lg" />
            </div>
          </div>

          <div className="space-y-2">
            <label htmlFor="review" className="text-sm font-medium">
              Review (optional)
            </label>
            <textarea
              id="review"
              rows={4}
              value={review}
              onChange={(e) => setReview(e.target.value)}
              placeholder="What did you think? Write your review..."
              className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 resize-none"
            />
          </div>

          <div className="flex items-center justify-end gap-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={pending}
            >
              {isEditing ? 'Cancel' : 'Skip'}
            </Button>
            <Button
              type="button"
              size="sm"
              onClick={handleSubmit}
              disabled={ratingValue < 1 || pending}
            >
              {pending && <span className="size-4 animate-spin rounded-full border-2 border-current border-t-transparent mr-1" />}
              {isEditing ? 'Update Rating' : 'Submit Rating'}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}