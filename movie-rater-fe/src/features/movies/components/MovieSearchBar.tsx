import { Search, SlidersHorizontal, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useMoviesStore } from '@src/stores/movies-store'
import { useEffect, useState } from 'react'
import { useDebouncedValue } from '@src/hooks/use-debounced-value'
import { DiscoverDialog } from './DiscoverDialog'

export function MovieSearchBar() {
  const searchQuery = useMoviesStore((s) => s.searchQuery)
  const setSearchQuery = useMoviesStore((s) => s.setSearchQuery)
  const setMode = useMoviesStore((s) => s.setMode)
  const [localQuery, setLocalQuery] = useState(searchQuery)
  const debouncedQuery = useDebouncedValue(localQuery, 350)
  const [discoverOpen, setDiscoverOpen] = useState(false)

  useEffect(() => {
    if (debouncedQuery.trim() !== searchQuery) {
      setSearchQuery(debouncedQuery)
      if (debouncedQuery.trim().length > 0) {
        setMode('search')
      } else {
        setMode('home')
      }
    }
  }, [debouncedQuery, searchQuery, setSearchQuery, setMode])

  const handleClear = () => {
    setLocalQuery('')
    setSearchQuery('')
    setMode('home')
  }

  return (
    <>
      <div className="flex items-center gap-2">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <Input
            value={localQuery}
            onChange={(e) => setLocalQuery(e.target.value)}
            placeholder="Search movies..."
            className="pl-9 pr-8"
          />
          {localQuery && (
            <button
              onClick={handleClear}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              <X className="size-4" />
            </button>
          )}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setDiscoverOpen(true)}
        >
          <SlidersHorizontal className="size-4" />
          Discover
        </Button>
      </div>
      <DiscoverDialog open={discoverOpen} onOpenChange={setDiscoverOpen} />
    </>
  )
}