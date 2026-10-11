import { useState } from 'react'
import { motion } from 'framer-motion'
import { Library, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useMovieLists } from '../hooks/use-movie-lists'
import { ListCard } from '../components/ListCard'
import { CreateListDialog } from '../components/ListFormDialog'
import { EditListDialog } from '../components/ListFormDialog'
import { DeleteListDialog } from '../components/DeleteListDialog'
import type { MovieListSummaryDto } from '@src/types/movie-list'
import { useI18n } from '@src/i18n/use-i18n'

export function ListsPage() {
  const { data, isLoading, isError, refetch } = useMovieLists()
  const { t } = useI18n()

  const [createOpen, setCreateOpen] = useState(false)
  const [editingList, setEditingList] = useState<MovieListSummaryDto | null>(null)
  const [deletingList, setDeletingList] = useState<MovieListSummaryDto | null>(null)

  if (isLoading) {
    return (
      <div className="p-6">
        <div className="flex items-center gap-3 mb-6">
          <Skeleton className="size-10 rounded-xl" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-3 w-48" />
          </div>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="space-y-3">
              <Skeleton className="aspect-[16/10] w-full rounded-lg" />
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3 w-1/2" />
            </div>
          ))}
        </div>
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Library className="size-16 text-muted-foreground/30" />
        <h2 className="text-xl font-semibold">{t('lists.failedToLoad')}</h2>
        <p className="text-muted-foreground">{t('lists.loadErrorDescription')}</p>
        <Button onClick={() => refetch()}>{t('common.tryAgain')}</Button>
      </div>
    )
  }

  const lists = data ?? []

  if (lists.length === 0) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Library className="size-16 text-muted-foreground/30" />
        <h2 className="text-xl font-semibold">{t('lists.noLists')}</h2>
        <p className="text-muted-foreground">
          {t('lists.noListsDescription')}
        </p>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="size-4" />
          {t('lists.createFirstList')}
        </Button>
        <CreateListDialog open={createOpen} onOpenChange={setCreateOpen} />
      </div>
    )
  }

  return (
    <div className="p-6 space-y-8">
      <div className="flex flex-wrap items-center gap-3 justify-between">
        <div className="flex items-center gap-3">
          <div className="relative flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10">
            <Library className="size-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold">{t('lists.heading')}</h1>
            <p className="text-sm text-muted-foreground">
              {t('lists.count', { count: lists.length })}
            </p>
          </div>
        </div>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="size-4" />
          {t('lists.newList')}
        </Button>
      </div>

      <motion.div layout className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {lists.map((list, index) => (
          <ListCard
            key={list.id}
            list={list}
            index={index}
            onEdit={() => setEditingList(list)}
            onDelete={() => setDeletingList(list)}
          />
        ))}
      </motion.div>

      <CreateListDialog open={createOpen} onOpenChange={setCreateOpen} />
      {editingList && (
        <EditListDialog open={!!editingList} onOpenChange={(open) => { if (!open) setEditingList(null) }} list={editingList} />
      )}
      <DeleteListDialog open={!!deletingList} onOpenChange={(open) => { if (!open) setDeletingList(null) }} list={deletingList} />
    </div>
  )
}