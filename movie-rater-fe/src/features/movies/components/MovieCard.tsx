import { Link } from 'react-router'
import { motion } from 'framer-motion'
import { Star } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { MoviePoster } from './MoviePoster'
import type { MovieSummaryDto } from '@src/types/movie'

interface MovieCardProps {
  movie: MovieSummaryDto
  index?: number
}

export function MovieCard({ movie, index = 0 }: MovieCardProps) {
  const year = movie.releaseDate ? movie.releaseDate.slice(0, 4) : null

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.05, ease: 'easeOut' }}
      whileHover={{ y: -4 }}
      className="group shrink-0 w-[160px]"
    >
      <Link to={`/movies/${movie.tmdbId}`} className="block space-y-2">
        <div className="relative overflow-hidden rounded-lg">
          <MoviePoster src={movie.posterUrl} alt={movie.title} />
          <div className="absolute top-2 right-2">
            <Badge variant="secondary" className="flex items-center gap-1 text-xs">
              <Star className="size-3 fill-yellow-500 text-yellow-500" />
              {movie.voteAverage.toFixed(1)}
            </Badge>
          </div>
        </div>
        <div className="space-y-0.5">
          <p className="text-sm font-medium leading-tight text-foreground line-clamp-2 group-hover:text-primary transition-colors">
            {movie.title}
          </p>
          {year && <p className="text-xs text-muted-foreground">{year}</p>}
        </div>
      </Link>
    </motion.div>
  )
}