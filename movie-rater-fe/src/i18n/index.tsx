import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  type PropsWithChildren,
} from 'react'
import { translate } from './lib'
import type { AppLocale, InterpolationParams, MessageKey } from './lib'
import {
  formatDate as formatDateValue,
  formatNumber as formatNumberValue,
  formatRuntime,
} from './format'
import { useLocaleStore } from '@src/stores/locale-store'

export { translate } from './lib'
export type { AppLocale, InterpolationParams, MessageKey, Messages } from './lib'
export type { PluralMessage } from './plural'

interface I18nContextValue {
  locale: AppLocale
  t: (key: MessageKey, params?: InterpolationParams) => string
  formatDate: (value: Date | string, options?: Intl.DateTimeFormatOptions) => string
  formatNumber: (value: number, options?: Intl.NumberFormatOptions) => string
  formatRuntime: (minutes: number) => string
}

const I18nContext = createContext<I18nContextValue | null>(null)

export function I18nProvider({ children }: PropsWithChildren) {
  const locale = useLocaleStore((s) => s.locale)

  useEffect(() => {
    document.documentElement.lang = locale
    document.title = translate(locale, 'app.title')
  }, [locale])

  const value = useMemo<I18nContextValue>(
    () => ({
      locale,
      t: (key, params) => translate(locale, key, params),
      formatDate: (date, options) => formatDateValue(locale, date, options),
      formatNumber: (value, options) => formatNumberValue(locale, value, options),
      formatRuntime,
    }),
    [locale],
  )

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n(): I18nContextValue {
  const context = useContext(I18nContext)
  if (!context) throw new Error('useI18n must be used within an I18nProvider')
  return context
}