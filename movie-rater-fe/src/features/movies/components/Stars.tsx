import { motion } from 'framer-motion'
import { Star } from 'lucide-react'

interface StarsProps {
  value: number
  onChange?: (value: number) => void
  size?: 'sm' | 'md' | 'lg'
  readonly?: boolean
}

const sizeMap = { sm: 'size-3.5', md: 'size-5', lg: 'size-7' }

export function Stars({ value, onChange, size = 'md', readonly = false }: StarsProps) {
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: 10 }, (_, i) => {
        const starValue = i + 1
        const filled = starValue <= value
        return (
          <motion.button
            key={starValue}
            type="button"
            whileHover={readonly ? undefined : { scale: 1.2 }}
            whileTap={readonly ? undefined : { scale: 0.9 }}
            onClick={() => {
              if (!readonly && onChange) onChange(starValue)
            }}
            className={`transition-colors ${
              readonly ? 'cursor-default' : 'cursor-pointer'
            } ${
              filled
                ? 'text-yellow-500'
                : 'text-muted-foreground/30 hover:text-yellow-500/50'
            }`}
            disabled={readonly}
            aria-label={`${starValue} star${starValue > 1 ? 's' : ''}`}
          >
            <Star className={sizeMap[size]} fill={filled ? 'currentColor' : 'none'} />
          </motion.button>
        )
      })}
    </div>
  )
}