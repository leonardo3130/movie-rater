import { motion } from 'framer-motion'
import { Link } from 'react-router'
import { Lock, Users, Pencil, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { ListCover } from './ListCover'
import type { MovieListSummaryDto } from '@src/types/movie-list'

interface ListCardProps {
  list: MovieListSummaryDto
  index?: number
  onEdit: () => void
  onDelete: () => void
}

export function ListCard({ list, index = 0, onEdit, onDelete }: ListCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.05, ease: 'easeOut' }}
      whileHover={{ y: -4 }}
      className="group relative"
    >
      <Link
        to={`/lists/${list.id}`}
        className="flex flex-col overflow-hidden rounded-xl bg-card text-sm text-card-foreground ring-1 ring-foreground/10 transition-shadow group-hover:shadow-md group-hover:ring-primary/30"
      >
        <ListCover id={list.id} name={list.name} movieCount={list.movieCount} />

        <div className="p-3 space-y-1">
          <p className="font-heading text-sm font-medium leading-snug text-foreground line-clamp-1 group-hover:text-primary transition-colors">
            {list.name}
          </p>
          {list.description && (
            <p className="text-xs text-muted-foreground leading-snug line-clamp-2">
              {list.description}
            </p>
          )}
        </div>

        <div className="flex items-center gap-1.5 px-3 pb-2.5 pt-1.5">
          <Badge variant="secondary" className="flex items-center gap-1 text-xs">
            <Users className="size-3" />
            {list.movieCount}
          </Badge>
          {list.isPrivate ? (
            <Badge variant="outline" className="flex items-center gap-1 text-[10px]">
              <Lock className="size-2.5" />
              Private
            </Badge>
          ) : (
            <Badge variant="outline" className="flex items-center gap-1 text-[10px]">
              <Users className="size-2.5" />
              Shared
            </Badge>
          )}
        </div>
      </Link>

      {list.isOwner && (
        <div className="absolute right-2 top-2 flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
          <motion.button
            type="button"
            whileTap={{ scale: 0.85 }}
            onClick={(e) => {
              e.preventDefault()
              e.stopPropagation()
              onEdit()
            }}
            className="rounded-full p-1.5 cursor-pointer transition-colors hover:bg-white/10 text-white/70 hover:text-white/95"
            aria-label={`Edit list ${list.name}`}
          >
            <Pencil className="size-3.5" />
          </motion.button>
          <motion.button
            type="button"
            whileTap={{ scale: 0.85 }}
            onClick={(e) => {
              e.preventDefault()
              e.stopPropagation()
              onDelete()
            }}
            className="rounded-full p-1.5 cursor-pointer transition-colors hover:bg-red-500/20 text-red-400"
            aria-label={`Delete list ${list.name}`}
          >
            <Trash2 className="size-3.5" />
          </motion.button>
        </div>
      )}
    </motion.div>
  )
}