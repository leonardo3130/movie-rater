import { Search, X } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { useTvStore } from '../stores/tv-store'
import { useEffect, useState } from 'react'
import { useDebouncedValue } from '@src/hooks/use-debounced-value'

export function TvSearchBar() {
  const searchQuery = useTvStore((s) => s.searchQuery)
  const setSearchQuery = useTvStore((s) => s.setSearchQuery)
  const browseGenre = useTvStore((s) => s.browseGenre)
  const setBrowseGenre = useTvStore((s) => s.setBrowseGenre)
  const [localQuery, setLocalQuery] = useState(searchQuery)
  const debouncedQuery = useDebouncedValue(localQuery, 350)

  useEffect(() => {
    if (browseGenre) {
      return
    }
    if (debouncedQuery.trim() !== searchQuery) {
      setSearchQuery(debouncedQuery)
    }
  }, [debouncedQuery, searchQuery, setSearchQuery, browseGenre])

  const value = browseGenre ? searchQuery : localQuery

  const handleChange = (query: string) => {
    if (browseGenre && query.trim()) {
      setBrowseGenre(null)
    }
    setLocalQuery(query)
  }

  const handleClear = () => {
    setLocalQuery('')
    setSearchQuery('')
    setBrowseGenre(null)
  }

  return (
    <div className="relative flex-1 max-w-md">
      <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
      <Input
        value={value}
        onChange={(e) => handleChange(e.target.value)}
        placeholder="Search TV shows..."
        className="pl-9 pr-8"
      />
      {value && (
        <button
          onClick={handleClear}
          className="absolute right-3 top-1/2 -translate-y-1/2 cursor-pointer text-muted-foreground hover:text-foreground"
          aria-label="Clear search"
        >
          <X className="size-4" />
        </button>
      )}
    </div>
  )
}