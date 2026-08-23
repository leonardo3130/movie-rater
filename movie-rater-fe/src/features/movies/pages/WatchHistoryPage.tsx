import { useState } from 'react'
import { motion } from 'framer-motion'
import { Link } from 'react-router'
import { Clock, MapPin, Trash2, Loader2, Star, Film, Calendar, AlertTriangle, UsersRound } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { MoviePoster } from '../components/MoviePoster'
import { useWatchSessions } from '../hooks/use-watch-sessions'
import { useDeleteWatchSession } from '../hooks/use-delete-watch-session'
import { useAuthStore } from '../../../stores/auth-store'
import { useGroups } from '../../groups/hooks/use-groups'

export function WatchHistoryPage() {
  const user = useAuthStore((s) => s.user)
  const [page, setPage] = useState(1)
  const deleteSession = useDeleteWatchSession()
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null)
  const [groupId, setGroupId] = useState<string | null>(null);
  const groups = useGroups()
  const selectedGroup = groups.data && groupId && !groups.isFetching ? groups.data.find(g => g.id == groupId) : undefined
  const { data, isLoading, isError, isFetching } = useWatchSessions({ page, pageSize: 20, groupId })

  const handleDeleteConfirm = () => {
    if (deleteTarget) {
      deleteSession.mutate(deleteTarget)
      setDeleteTarget(null)
    }
  }

  if (isLoading || groups.isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="size-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  if (isError || groups.isError) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">Failed to load watch history</p>
      </div>
    )
  }

  const sessions = data?.items ?? []

  if (sessions.length === 0 && !groupId) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4">
        <Film className="size-16 text-muted-foreground/30" />
        <h2 className="text-xl font-semibold">No watch sessions yet</h2>
        <p className="text-muted-foreground">
          Start watching movies and mark them as watched!
        </p>
        <Link to="/movies">
          <Button>Browse movies</Button>
        </Link>
      </div>
    )
  }

  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 1

  console.log(groups.data)

  return (
    <div className="p-6 space-y-8">
      <div className="flex items-center gap-3">
        <div className="relative flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10">
          <Clock className="size-5 text-primary" />
        </div>
        <div className="flex justify-between w-full">
          <div>
            <h1 className="text-2xl font-bold">Watch History</h1>
            <p className="text-sm text-muted-foreground">
              {data?.totalCount ?? 0} session{data?.totalCount !== 1 ? 's' : ''}
            </p>
          </div>

          <div className='space-y-2'>
            <Label htmlFor="groupId">
              <UsersRound className="size-3.5 text-muted-foreground" />
              Group
            </Label>
            <Select value={groupId} onValueChange={(value) => {
              setGroupId(value)
            }} >
              <SelectTrigger className="w-full max-w-48">
                {/*displyed value*/}
                <SelectValue placeholder="Select group" >
                  {selectedGroup?.name}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectLabel>Groups</SelectLabel>
                  {!(groups.isPending || groups.isLoading) && groups.data && groups.data.map(g =>
                    <SelectItem key={g.id} value={g.id}>{g.name}</SelectItem>)}
                  <SelectItem value={null}>
                    All groups
                  </SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
        </div>

        <Dialog open={deleteTarget !== null} onOpenChange={(o) => { if (!o) setDeleteTarget(null) }}>
          <DialogContent className="max-w-sm">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <AlertTriangle className="size-5 text-destructive" />
                Delete Watch Session?
              </DialogTitle>
              <DialogDescription>
                This will permanently remove this watch session and all its ratings. This action cannot be undone.
              </DialogDescription>
            </DialogHeader>
            <div className="flex justify-end gap-2 pt-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setDeleteTarget(null)}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={handleDeleteConfirm}
                disabled={deleteSession.isPending}
              >
                {deleteSession.isPending && <Loader2 className="size-4 animate-spin" />}
                Delete
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <div className="space-y-3">
        {sessions.map((session, index) => {
          const watchedDate = new Date(session.watchedAt)
          const formattedDate = watchedDate.toLocaleDateString('en-US', {
            weekday: 'short',
            year: 'numeric',
            month: 'short',
            day: 'numeric',
          })
          const isCreator = session.createdByUserId === user?.id

          return (
            <motion.div
              key={session.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.2, delay: index * 0.03 }}
            >
              <Link
                to={`/watch-history/${session.id}`}
                className="flex items-center gap-4 p-3 rounded-lg border border-border/50 bg-card hover:bg-accent/50 transition-colors group"
              >
                <div className="w-14 shrink-0">
                  <MoviePoster src={session.moviePosterUrl} alt={session.movieTitle} />
                </div>

                <div className="flex-1 min-w-0 space-y-1">
                  <p className="font-medium text-sm leading-tight line-clamp-1 group-hover:text-primary transition-colors">
                    {session.movieTitle}
                  </p>
                  <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <Calendar className="size-3" />
                      {formattedDate}
                    </span>
                    {session.location && (
                      <span className="flex items-center gap-1">
                        <MapPin className="size-3" />
                        {session.location}
                      </span>
                    )}
                    <span className="flex items-center gap-1">
                      <Star className="size-3" />
                      {session.ratingCount} rating{session.ratingCount !== 1 ? 's' : ''}
                    </span>
                  </div>
                  {session.notes && (
                    <p className="text-xs text-muted-foreground line-clamp-1 italic">
                      &ldquo;{session.notes}&rdquo;
                    </p>
                  )}
                  {isCreator && (
                    <Badge variant="outline" className="text-[10px] py-0 h-4">
                      You created this
                    </Badge>
                  )}
                </div>

                {isCreator && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="shrink-0 text-muted-foreground hover:text-destructive transition-all opacity-0 group-hover:opacity-100 max-sm:opacity-100"
                    onClick={(e) => {
                      e.preventDefault()
                      e.stopPropagation()
                      setDeleteTarget(session.id)
                    }}
                    disabled={deleteSession.isPending}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                )}
              </Link>
            </motion.div>
          )
        })}
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
          <span className="text-sm text-muted-foreground tabular-nums">
            {page} / {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages || isFetching}
            onClick={() => setPage((p) => p + 1)}
          >
            {isFetching && <Loader2 className="size-3 animate-spin mr-1" />}
            Next
          </Button>
        </div>
      )}
    </div>
  )
}
