import { Loader2, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useDeleteMovieList } from '../hooks/use-delete-movie-list'
import type { MovieListBaseDto } from '@src/types/movie-list'
import { useI18n } from '@src/i18n/use-i18n'

interface DeleteListDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  list: MovieListBaseDto | null
  onSuccess?: () => void
}

export function DeleteListDialog({ open, onOpenChange, list, onSuccess }: DeleteListDialogProps) {
  const deleteList = useDeleteMovieList()
  const { t } = useI18n()

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
          <DialogTitle>{t('lists.delete.title')}</DialogTitle>
          <DialogDescription>
            {t('lists.delete.description', {
              name: list?.name ?? t('lists.delete.thisList'),
            })}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)} disabled={deleteList.isPending}>
            {t('common.cancel')}
          </Button>
          <Button variant="destructive" size="sm" onClick={handleDelete} disabled={deleteList.isPending}>
            {deleteList.isPending && <Loader2 className="size-4 animate-spin" />}
            <Trash2 className="size-4" />
            {t('lists.delete.confirm')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}