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
import { useI18n } from '@src/i18n/use-i18n'

interface DiscoverDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function DiscoverDialog({ open, onOpenChange }: DiscoverDialogProps) {
  const { data: genresData } = useGenres()
  const { t } = useI18n()
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
          <DialogTitle>{t('movies.discover.title')}</DialogTitle>
          <DialogDescription>
            {t('movies.discover.description')}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          <div className="space-y-2">
            <Label>{t('movies.discover.genres')}</Label>
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
            <Label htmlFor="primaryReleaseYear">{t('movies.discover.releaseYear')}</Label>
            <Input
              id="primaryReleaseYear"
              placeholder={t('movies.discover.releaseYearPlaceholder')}
              {...register('primaryReleaseYear')}
            />
            {errors.primaryReleaseYear && (
              <p className="text-xs text-destructive">{errors.primaryReleaseYear.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="primaryReleaseDateGte">{t('movies.discover.fromDate')}</Label>
              <Input
                id="primaryReleaseDateGte"
                type="date"
                {...register('primaryReleaseDateGte')}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="primaryReleaseDateLte">{t('movies.discover.toDate')}</Label>
              <Input
                id="primaryReleaseDateLte"
                type="date"
                {...register('primaryReleaseDateLte')}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label>
            {t('movies.discover.minimumRating', { value: voteAverageGte?.toFixed(1) ?? '0.0' })}
          </Label>
            <Slider
              min={0}
              max={10}
              step={0.5}
              value={[voteAverageGte ?? 0]}
              onValueChange={(value: number | readonly number[]) => setValue('voteAverageGte', Array.isArray(value) ? value[0] : value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="sortBy">{t('movies.discover.sortBy')}</Label>
            <Select
              value={sortBy}
              onValueChange={(v) => setValue('sortBy', v as DiscoverFormValues['sortBy'])}
            >
              <SelectTrigger className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="popularity.desc">{t('movies.discover.popularityHighToLow')}</SelectItem>
                <SelectItem value="popularity.asc">{t('movies.discover.popularityLowToHigh')}</SelectItem>
                <SelectItem value="vote_average.desc">{t('movies.discover.ratingHighToLow')}</SelectItem>
                <SelectItem value="vote_average.asc">{t('movies.discover.ratingLowToHigh')}</SelectItem>
                <SelectItem value="primary_release_date.desc">{t('movies.discover.releaseDateNewest')}</SelectItem>
                <SelectItem value="primary_release_date.asc">{t('movies.discover.releaseDateOldest')}</SelectItem>
                <SelectItem value="revenue.desc">{t('movies.discover.revenueHighToLow')}</SelectItem>
                <SelectItem value="revenue.asc">{t('movies.discover.revenueLowToHigh')}</SelectItem>
                <SelectItem value="original_title.asc">{t('movies.discover.titleAZ')}</SelectItem>
                <SelectItem value="original_title.desc">{t('movies.discover.titleZA')}</SelectItem>
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
              {t('movies.discover.includeAdult')}
            </Label>
          </div>

          <div className="flex items-center justify-between pt-2">
            <Button type="button" variant="ghost" size="sm" onClick={handleReset}>
              {t('common.reset')}
            </Button>
            <Button type="submit" size="sm">
              <Search className="size-4" />
              {t('common.search')}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}