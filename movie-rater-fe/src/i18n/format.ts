type DateTimeOptions = Intl.DateTimeFormatOptions

const dateFormatters = new Map<string, Intl.DateTimeFormat>()
const numberFormatters = new Map<string, Intl.NumberFormat>()

export function formatDate(
  locale: string,
  value: Date | string,
  options?: DateTimeOptions,
): string {
  const key = `${locale}:${options ? JSON.stringify(options) : 'default'}`
  let formatter = dateFormatters.get(key)
  if (!formatter) {
    formatter = options ? new Intl.DateTimeFormat(locale, options) : new Intl.DateTimeFormat(locale)
    dateFormatters.set(key, formatter)
  }
  return formatter.format(typeof value === 'string' ? new Date(value) : value)
}

export function formatNumber(
  locale: string,
  value: number,
  options?: Intl.NumberFormatOptions,
): string {
  const key = `${locale}:${options ? JSON.stringify(options) : 'default'}`
  let formatter = numberFormatters.get(key)
  if (!formatter) {
    formatter = options
      ? new Intl.NumberFormat(locale, options)
      : new Intl.NumberFormat(locale)
    numberFormatters.set(key, formatter)
  }
  return formatter.format(value)
}

export function formatCurrency(locale: string, value: number): string {
  return formatNumber(locale, value, {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  })
}

export function formatRuntime(minutes: number): string {
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  if (hours === 0) return `${rest}m`
  if (rest === 0) return `${hours}h`
  return `${hours}h ${rest}m`
}