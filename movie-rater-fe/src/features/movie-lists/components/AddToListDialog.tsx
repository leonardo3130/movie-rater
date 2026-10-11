import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, ListPlus, Plus, CheckCircle2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { MoviePoster } from '../../movies/components/MoviePoster'
import { Select, SelectContent, SelectGroup, SelectItem, SelectLabel, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useMovieLists } from '../hooks/use-movie-lists'
import { useCreateMovieList } from '../hooks/use-create-movie-list'
import { useAddMovieToList } from '../hooks/use-add-movie-to-list'
import { quickListSchema, type QuickListFormValues } from '../schemas/movie-list.schema'
import { useI18n } from '@src/i18n/use-i18n'

interface AddToListDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  movieId: string
  tmdbId: number
  movieTitle: string
  moviePosterUrl?: string | null
}

export function AddToListDialog({
  open,
  onOpenChange,
  movieId,
  tmdbId,
  movieTitle,
  moviePosterUrl,
}: AddToListDialogProps) {
  const listsQuery = useMovieLists()
  const createList = useCreateMovieList()
  const addMovie = useAddMovieToList()
  const { t } = useI18n()

  const [selectedOverride, setSelectedOverride] = useState('')
  const [showQuickCreate, setShowQuickCreate] = useState(false)

  const lists = listsQuery.data ?? []
  const selectedListId = selectedOverride || lists[0]?.id || ''
  const selectedList = lists.find((list) => list.id === selectedListId)

  const handleClose = (next: boolean) => {
    if (!next) {
      setSelectedOverride('')
      setShowQuickCreate(false)
    }
    onOpenChange(next)
  }

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<QuickListFormValues>({
    resolver: zodResolver(quickListSchema),
    defaultValues: { name: '' },
  })

  const handleAdd = () => {
    if (!selectedListId) return
    addMovie.mutate(
      { listId: selectedListId, movieId, tmdbId },
      { onSuccess: () => onOpenChange(false) },
    )
  }

  const handleQuickCreate = (values: QuickListFormValues) => {
    createList.mutate(
      {
        name: values.name,
        description: null,
        isPrivate: true,
        canGroupEdit: false,
        groupIds: [],
      },
      {
        onSuccess: (created) => {
          addMovie.mutate(
            { listId: created.id, movieId, tmdbId },
            { onSuccess: () => onOpenChange(false) },
          )
        },
      },
    )
  }

  const listsLoaded = !listsQuery.isLoading && !listsQuery.isPending
  const pending = isSubmitting || createList.isPending || addMovie.isPending

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-md max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t('lists.addToList.title')}</DialogTitle>
          <DialogDescription>
            {t('lists.addToList.description')}
          </DialogDescription>
        </DialogHeader>

        <div className="flex items-center gap-3 mb-4 p-3 rounded-lg bg-muted/50">
          <div className="w-12 shrink-0">
            <MoviePoster src={moviePosterUrl ?? null} alt={movieTitle} />
          </div>
          <div className="min-w-0">
            <p className="font-medium text-sm leading-tight line-clamp-2">{movieTitle}</p>
          </div>
        </div>

        {!listsLoaded && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="size-6 animate-spin text-muted-foreground" />
          </div>
        )}

        {listsLoaded && !showQuickCreate && lists.length > 0 && (
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="listId">{t('lists.addToList.list')}</Label>
              <Select value={selectedListId} onValueChange={(value) => { if (value) setSelectedOverride(value) }}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder={t('lists.addToList.selectList')}>{selectedList?.name}</SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectGroup>
                    <SelectLabel>{t('lists.addToList.yourLists')}</SelectLabel>
                    {lists.map((list) => (
                      <SelectItem key={list.id} value={list.id}>
                        {list.name}
                      </SelectItem>
                    ))}
                  </SelectGroup>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2">
              <Button type="button" variant="ghost" size="sm" onClick={() => setShowQuickCreate(true)}>
                <Plus className="size-4" />
                {t('lists.addToList.newList')}
              </Button>
              <Button type="button" size="sm" disabled={!selectedListId || pending} onClick={handleAdd}>
                {pending && <Loader2 className="size-4 animate-spin" />}
                <ListPlus className="size-4" />
                {t('lists.addToList.add')}
              </Button>
            </div>
          </div>
        )}

        {listsLoaded && (showQuickCreate || lists.length === 0) && (
          <form onSubmit={handleSubmit(handleQuickCreate)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="quickListName">{t('lists.addToList.newListName')}</Label>
              <Input
                id="quickListName"
                placeholder={t('lists.addToList.namePlaceholder')}
                aria-invalid={!!errors.name}
                {...register('name')}
              />
              {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
            </div>

            <div className="flex items-center justify-end gap-2 pt-2">
              {lists.length > 0 && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={pending}
                  onClick={() => {
                    reset()
                    setShowQuickCreate(false)
                  }}
                >
                  {t('lists.addToList.back')}
                </Button>
              )}
              <Button type="submit" size="sm" disabled={pending}>
                {pending && <Loader2 className="size-4 animate-spin" />}
                <CheckCircle2 className="size-4" />
                {t('lists.addToList.createAndAdd')}
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  )
}