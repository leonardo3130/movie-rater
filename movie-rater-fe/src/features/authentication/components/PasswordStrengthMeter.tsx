import { motion } from 'framer-motion'
import { scorePassword } from '../../../lib/password-strength'

interface Props {
  password: string
}

export function PasswordStrengthMeter({ password }: Props) {
  if (!password) return null

  const { score, label, color } = scorePassword(password)
  const segments = [1, 2, 3, 4]
  const filledSegments = score

  return (
    <div className="mt-1 space-y-1">
      <div className="flex gap-1">
        {segments.map((segment) => (
          <motion.div
            key={segment}
            layout
            transition={{ type: 'spring', stiffness: 300, damping: 25 }}
            className={`h-1 flex-1 rounded-full transition-colors duration-300 ${
              segment <= filledSegments ? color : 'bg-muted'
            }`}
          />
        ))}
      </div>
      <p className="text-xs text-muted-foreground">{label}</p>
    </div>
  )
}