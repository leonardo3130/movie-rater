import { Library } from 'lucide-react'
import { MoviePoster } from '../../movies/components/MoviePoster'

const GRADIENTS = [
  'from-primary/70 to-primary/10',
  'from-red-500/70 to-orange-400/10',
  'from-emerald-500/70 to-teal-400/10',
  'from-blue-500/70 to-indigo-400/10',
  'from-violet-500/70 to-fuchsia-400/10',
  'from-amber-500/70 to-yellow-400/10',
  'from-rose-500/70 to-pink-400/10',
  'from-cyan-500/70 to-sky-400/10',
]

function hashCode(input: string) {
  let hash = 0
  for (let i = 0; i < input.length; i++) {
    hash = (hash * 31 + input.charCodeAt(i)) & 0x7fffffff
  }
  return hash
}

interface ListCoverProps {
  id: string
  movieCount: number
  posters?: Array<string | null>
}

export function ListCover({ id, movieCount, posters }: ListCoverProps) {
  const gradient = GRADIENTS[hashCode(id) % GRADIENTS.length]
  const visible = (posters ?? []).slice(0, 4).filter(Boolean) as string[]

  if (visible.length > 0) {
    return (
      <div className="aspect-[16/10] overflow-hidden rounded-lg bg-muted">
        {visible.length === 1 ? (
          <MoviePoster src={visible[0]} alt="" className="size-full" />
        ) : visible.length === 2 ? (
          <div className="grid grid-cols-2 size-full">
            {visible.map((src) => (
              <MoviePoster key={src} src={src} alt="" className="size-full" />
            ))}
          </div>
        ) : visible.length === 3 ? (
          <div className="grid grid-cols-2 size-full">
            <MoviePoster src={visible[0]} alt="" className="size-full row-span-2" />
            <MoviePoster src={visible[1]} alt="" className="size-full" />
            <MoviePoster src={visible[2]} alt="" className="size-full" />
          </div>
        ) : (
          <div className="grid grid-cols-2 grid-rows-2 size-full">
            {visible.map((src) => (
              <MoviePoster key={src} src={src} alt="" className="size-full" />
            ))}
          </div>
        )}
      </div>
    )
  }

  return (
    <div className={`aspect-[16/10] overflow-hidden rounded-lg bg-gradient-to-br ${gradient}`}>
      <div className="flex size-full flex-col items-center justify-center gap-1.5 text-primary-foreground/90">
        <Library className="size-8" strokeWidth={1.5} />
        <span className="text-xs font-medium tabular-nums">
          {movieCount} movie{movieCount === 1 ? '' : 's'}
        </span>
      </div>
    </div>
  )
}