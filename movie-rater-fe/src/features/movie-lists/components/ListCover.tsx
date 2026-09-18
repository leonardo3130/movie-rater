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

function getInitials(name: string, max = 4) {
  const words = name.trim().split(/\s+/).filter(Boolean)
  if (words.length >= 2) {
    return words.slice(0, max).map((w) => w[0]).join('').toUpperCase()
  }
  return name.trim().slice(0, max).toUpperCase()
}

function initialsSize(initials: string) {
  if (initials.length >= 4) return 'text-2xl sm:text-3xl'
  if (initials.length === 3) return 'text-3xl sm:text-4xl'
  return 'text-4xl sm:text-5xl'
}

interface ListCoverProps {
  id: string
  name: string
  movieCount: number
  posters?: Array<string | null>
}

export function ListCover({ id, name, movieCount, posters }: ListCoverProps) {
  const gradient = GRADIENTS[hashCode(id) % GRADIENTS.length]
  const visible = (posters ?? []).slice(0, 4).filter(Boolean) as string[]
  const initials = getInitials(name)

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
      <div className="flex size-full items-center justify-center">
        <span
          className={`font-heading font-bold tracking-tight text-primary-foreground/95 ${initialsSize(initials)}`}
          aria-hidden="true"
        >
          {initials || movieCount}
        </span>
      </div>
    </div>
  )
}