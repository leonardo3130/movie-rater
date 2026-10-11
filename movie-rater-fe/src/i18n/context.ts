import { createContext } from 'react'
import type { AppLocale, InterpolationParams, MessageKey } from './lib'

export interface I18nContextValue {
  locale: AppLocale
  t: (key: MessageKey, params?: InterpolationParams) => string
  formatDate: (value: Date | string, options?: Intl.DateTimeFormatOptions) => string
  formatNumber: (value: number, options?: Intl.NumberFormatOptions) => string
  formatCurrency: (value: number) => string
  formatRuntime: (minutes: number) => string
}

export const I18nContext = createContext<I18nContextValue | null>(null)