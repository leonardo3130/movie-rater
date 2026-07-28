import { motion } from 'framer-motion'
import { Heart, Bookmark } from 'lucide-react'
import { useToggleFavorite } from '../hooks/use-toggle-favorite'
import { useToggleWatchlist } from '../hooks/use-toggle-watchlist'

interface UserMovieToggleProps {
  movieId: string
  isFavorite?: boolean
  isInWatchlist?: boolean
  size?: 'sm' | 'md' | 'lg'
}

const sizeMap = {
  sm: 'size-3.5',
  md: 'size-4',
  lg: 'size-5',
}

const buttonSizeMap = {
  sm: 'p-1',
  md: 'p-1.5',
  lg: 'p-2',
}

export function UserMovieToggle({
  movieId,
  isFavorite = false,
  isInWatchlist = false,
  size = 'md',
}: UserMovieToggleProps) {
  const toggleFavorite = useToggleFavorite()
  const toggleWatchlist = useToggleWatchlist()

  return (
    <div className="flex items-center gap-1">
      <motion.button
        type="button"
        whileTap={{ scale: 0.8 }}
        onClick={(e) => {
          e.preventDefault()
          e.stopPropagation()
          toggleFavorite.mutate({ movieId, value: !isFavorite })
        }}
        className={`rounded-full ${buttonSizeMap[size]} transition-colors hover:bg-white/10 ${
          isFavorite ? 'text-red-500' : 'text-white/60 hover:text-white/90'
        }`}
        aria-label={isFavorite ? 'Remove from favorites' : 'Add to favorites'}
      >
        <Heart
          className={sizeMap[size]}
          fill={isFavorite ? 'currentColor' : 'none'}
        />
      </motion.button>

      <motion.button
        type="button"
        whileTap={{ scale: 0.8 }}
        onClick={(e) => {
          e.preventDefault()
          e.stopPropagation()
          toggleWatchlist.mutate({ movieId, value: !isInWatchlist })
        }}
        className={`rounded-full ${buttonSizeMap[size]} transition-colors hover:bg-white/10 ${
          isInWatchlist ? 'text-yellow-400' : 'text-white/60 hover:text-white/90'
        }`}
        aria-label={isInWatchlist ? 'Remove from watchlist' : 'Add to watchlist'}
      >
        <Bookmark
          className={sizeMap[size]}
          fill={isInWatchlist ? 'currentColor' : 'none'}
        />
      </motion.button>
    </div>
  )
}