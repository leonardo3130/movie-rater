import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Search, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Slider } from '@/components/ui/slider'
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { useGenres } from '../hooks/use-genres'
import { useMoviesStore } from '@src/stores/movies-store'
import { discoverSchema, type DiscoverFormValues } from '../schemas/discover.schema'

interface DiscoverDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function DiscoverDialog({ open, onOpenChange }: DiscoverDialogProps) {
  const { data: genresData } = useGenres()
  const setDiscoverFilters = useMoviesStore((s) => s.setDiscoverFilters)
  const setPage = useMoviesStore((s) => s.setPage)
  const setMode = useMoviesStore((s) => s.setMode)
  const resetDiscoverFilters = useMoviesStore((s) => s.resetDiscoverFilters)
  const storeFilters = useMoviesStore((s) => s.discoverFilters)

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<DiscoverFormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(discoverSchema) as any,
    defaultValues: {
      genreIds: storeFilters.genreIds,
      primaryReleaseYear: storeFilters.primaryReleaseYear,
      primaryReleaseDateGte: storeFilters.primaryReleaseDateGte,
      primaryReleaseDateLte: storeFilters.primaryReleaseDateLte,
      voteAverageGte: storeFilters.voteAverageGte,
      sortBy: storeFilters.sortBy,
      includeAdult: storeFilters.includeAdult,
    },
  })

  const selectedGenres = watch('genreIds')
  const voteAverageGte = watch('voteAverageGte')

  const toggleGenre = (tmdbId: number) => {
    const current = selectedGenres ?? []
    const next = current.includes(tmdbId)
      ? current.filter((id) => id !== tmdbId)
      : [...current, tmdbId]
    setValue('genreIds', next, { shouldValidate: true })
  }

  const sortBy = watch('sortBy')

  const onSubmit = (values: DiscoverFormValues) => {
    setDiscoverFilters(values)
    setPage('discover', 1)
    setMode('discover')
    onOpenChange(false)
  }

  const handleReset = () => {
    reset({
      genreIds: [],
      primaryReleaseYear: '',
      primaryReleaseDateGte: '',
      primaryReleaseDateLte: '',
      voteAverageGte: 0,
      sortBy: 'popularity.desc',
      includeAdult: false,
    })
    resetDiscoverFilters()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Discover Movies</DialogTitle>
          <DialogDescription>
            Filter movies by genre, year, rating, and more
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          <div className="space-y-2">
            <Label>Genres</Label>
            <div className="flex flex-wrap gap-1.5">
              {genresData?.genres.map((genre) => {
                const isSelected = selectedGenres?.includes(genre.tmdbId)
                return (
                  <Badge
                    key={genre.tmdbId}
                    variant={isSelected ? 'default' : 'outline'}
                    className="cursor-pointer select-none"
                    onClick={() => toggleGenre(genre.tmdbId)}
                  >
                    {genre.name}
                    {isSelected && <X className="size-3 ml-1" />}
                  </Badge>
                )
              })}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="primaryReleaseYear">Release Year</Label>
            <Input
              id="primaryReleaseYear"
              placeholder="e.g. 2024"
              {...register('primaryReleaseYear')}
            />
            {errors.primaryReleaseYear && (
              <p className="text-xs text-destructive">{errors.primaryReleaseYear.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="primaryReleaseDateGte">From Date</Label>
              <Input
                id="primaryReleaseDateGte"
                type="date"
                {...register('primaryReleaseDateGte')}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="primaryReleaseDateLte">To Date</Label>
              <Input
                id="primaryReleaseDateLte"
                type="date"
                {...register('primaryReleaseDateLte')}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label>Minimum Rating: {voteAverageGte?.toFixed(1) ?? '0.0'}</Label>
            <Slider
              min={0}
              max={10}
              step={0.5}
              value={[voteAverageGte ?? 0]}
              onValueChange={(value: number | readonly number[]) => setValue('voteAverageGte', Array.isArray(value) ? value[0] : value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="sortBy">Sort By</Label>
            <Select
              value={sortBy}
              onValueChange={(v) => setValue('sortBy', v as DiscoverFormValues['sortBy'])}
            >
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="popularity.desc">Popularity (High to Low)</SelectItem>
                <SelectItem value="popularity.asc">Popularity (Low to High)</SelectItem>
                <SelectItem value="vote_average.desc">Rating (High to Low)</SelectItem>
                <SelectItem value="vote_average.asc">Rating (Low to High)</SelectItem>
                <SelectItem value="primary_release_date.desc">Release Date (Newest)</SelectItem>
                <SelectItem value="primary_release_date.asc">Release Date (Oldest)</SelectItem>
                <SelectItem value="revenue.desc">Revenue (High to Low)</SelectItem>
                <SelectItem value="revenue.asc">Revenue (Low to High)</SelectItem>
                <SelectItem value="original_title.asc">Title (A-Z)</SelectItem>
                <SelectItem value="original_title.desc">Title (Z-A)</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="includeAdult"
              className="size-4 rounded border-border accent-primary"
              {...register('includeAdult')}
            />
            <Label htmlFor="includeAdult" className="text-sm font-normal cursor-pointer">
              Include adult content
            </Label>
          </div>

          <div className="flex items-center justify-between pt-2">
            <Button type="button" variant="ghost" size="sm" onClick={handleReset}>
              Reset
            </Button>
            <Button type="submit" size="sm">
              <Search className="size-4" />
              Search
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}