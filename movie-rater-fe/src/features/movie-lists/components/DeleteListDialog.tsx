import { Loader2, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useDeleteMovieList } from '../hooks/use-delete-movie-list'
import type { MovieListBaseDto } from '@src/types/movie-list'

interface DeleteListDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  list: MovieListBaseDto | null
  onSuccess?: () => void
}

export function DeleteListDialog({ open, onOpenChange, list, onSuccess }: DeleteListDialogProps) {
  const deleteList = useDeleteMovieList()

  const handleDelete = () => {
    if (!list) return
    deleteList.mutate(list.id, {
      onSuccess: () => {
        onOpenChange(false)
        onSuccess?.()
      },
    })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md max-h-[40vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Delete list?</DialogTitle>
          <DialogDescription>
            &quot;{list?.name ?? 'This list'}&quot; and all of its movies will be permanently removed.
            This action cannot be undone.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)} disabled={deleteList.isPending}>
            Cancel
          </Button>
          <Button variant="destructive" size="sm" onClick={handleDelete} disabled={deleteList.isPending}>
            {deleteList.isPending && <Loader2 className="size-4 animate-spin" />}
            <Trash2 className="size-4" />
            Delete list
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}