import { motion } from 'framer-motion'

interface Props {
  children: React.ReactNode
  title: string
  subtitle?: string
}

export function AuthLayout({ children, title, subtitle }: Props) {
  return (
    <div className="flex min-h-dvh flex-col md:flex-row">
      <div className="relative hidden flex-1 items-center justify-center overflow-hidden bg-linear-to-br from-primary/20 via-background to-primary/10 md:flex">
        <div className="max-w-md space-y-4 px-8 text-center">
          <motion.h1
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, ease: 'easeOut' }}
            className="font-heading text-4xl font-bold tracking-tight"
          >
            Movie Rater
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.15, ease: 'easeOut' }}
            className="text-muted-foreground"
          >
            Track the movies you watch together. Rate, review, and discover your shared cinematic journey.
          </motion.p>
        </div>
      </div>

      <motion.div
        initial={{ opacity: 0, x: 20 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        className="flex flex-1 items-center justify-center px-4 py-12 md:px-8"
      >
        <div className="w-full max-w-sm space-y-6">
          <div className="text-center md:text-left">
            <h1 className="font-heading text-2xl font-bold tracking-tight md:hidden">
              Movie Rater
            </h1>
            <h2 className="text-xl font-semibold">{title}</h2>
            {subtitle && (
              <p className="mt-1 text-sm text-muted-foreground">{subtitle}</p>
            )}
          </div>
          {children}
        </div>
      </motion.div>
    </div>
  )
}
