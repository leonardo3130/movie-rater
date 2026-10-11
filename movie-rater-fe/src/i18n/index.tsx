import { useEffect, useMemo, type PropsWithChildren } from 'react'
import { translate } from './lib'
import { I18nContext } from './context'
import type { I18nContextValue } from './context'
import {
  formatDate as formatDateValue,
  formatCurrency as formatCurrencyValue,
  formatNumber as formatNumberValue,
  formatRuntime,
} from './format'
import { useLocaleStore } from '@src/stores/locale-store'

export type { AppLocale, InterpolationParams, MessageKey, Messages } from './lib'
export type { PluralMessage } from './plural'

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
      formatCurrency: (value) => formatCurrencyValue(locale, value),
      formatRuntime,
    }),
    [locale],
  )

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}