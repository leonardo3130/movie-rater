const COMMON_PATTERNS = [
  'password', '123456', '12345678', '123456789', 'qwerty',
  'abc123', 'letmein', 'welcome', 'monkey', 'dragon',
  'master', 'admin', 'login', 'passw0rd', 'shadow',
  'sunshine', 'princess', 'football', 'iloveyou', 'trustno1',
]

const SEQUENCES = 'abcdefghijklmnopqrstuvwxyz0123456789'

export interface PasswordScore {
  score: 0 | 1 | 2 | 3 | 4
  label: string
  color: string
}

function hasSequence(password: string, length: number): boolean {
  const lower = password.toLowerCase()
  for (let i = 0; i <= lower.length - length; i++) {
    const segment = lower.slice(i, i + length)
    if (SEQUENCES.includes(segment)) return true
  }
  return false
}

function hasRepeatingChars(password: string): boolean {
  return /(.)\1{2,}/.test(password)
}

export function scorePassword(password: string): PasswordScore {
  if (!password) return { score: 0, label: '', color: '' }

  let score = 0

  if (password.length >= 8) score += 1
  if (password.length >= 12) score += 1
  if (password.length >= 16) score += 1

  if (/[a-z]/.test(password) && /[A-Z]/.test(password)) score += 1
  if (/\d/.test(password)) score += 1
  if (/[^a-zA-Z0-9]/.test(password)) score += 1

  const lower = password.toLowerCase()
  if (COMMON_PATTERNS.some((p) => lower.includes(p))) score = Math.max(0, score - 2)
  if (hasSequence(password, 3)) score = Math.max(0, score - 1)
  if (hasRepeatingChars(password)) score = Math.max(0, score - 1)

  const clamped = Math.max(0, Math.min(4, score)) as 0 | 1 | 2 | 3 | 4

  const LABELS: Record<number, string> = {
    0: 'Weak',
    1: 'Weak',
    2: 'Fair',
    3: 'Good',
    4: 'Strong',
  }

  const COLORS: Record<number, string> = {
    0: 'bg-destructive',
    1: 'bg-destructive',
    2: 'bg-chart-3',
    3: 'bg-chart-2',
    4: 'bg-chart-1',
  }

  return { score: clamped, label: LABELS[clamped], color: COLORS[clamped] }
}
