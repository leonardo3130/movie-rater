import { useState } from 'react'
import { motion } from 'framer-motion'
import { Skeleton } from '@/components/ui/skeleton'

interface MoviePosterProps {
  src: string | null
  alt: string
  className?: string
}

export function MoviePoster({ src, alt, className = '' }: MoviePosterProps) {
  const [loaded, setLoaded] = useState(false)
  const [error, setError] = useState(false)

  const showPlaceholder = !loaded || error || !src

  return (
    <div className={`relative aspect-[2/3] overflow-hidden rounded-lg bg-muted ${className}`}>
      {showPlaceholder && <Skeleton className="absolute inset-0 size-full" />}
      {src && (
        <motion.img
          src={src}
          alt={alt}
          initial={{ opacity: 0 }}
          animate={{ opacity: loaded ? 1 : 0 }}
          transition={{ duration: 0.3 }}
          onLoad={() => setLoaded(true)}
          onError={() => setError(true)}
          className="size-full object-cover"
        />
      )}
    </div>
  )
}