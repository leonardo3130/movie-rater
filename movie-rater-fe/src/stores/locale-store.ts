import { create } from 'zustand'
import { APP_LOCALES, translate } from '@src/i18n/lib'
import type { AppLocale, InterpolationParams, MessageKey } from '@src/i18n/lib'

const STORAGE_KEY = 'mr_locale'
const DEFAULT_LOCALE: AppLocale = 'en'

function isSupported(value: string | null): value is AppLocale {
  return value != null && value in APP_LOCALES
}

function detectInitialLocale(): AppLocale {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (isSupported(stored)) return stored

  const browserLocale =
    typeof navigator !== 'undefined' ? (navigator.language ?? '').toLowerCase() : ''
  if (browserLocale.startsWith('it')) return 'it'

  return DEFAULT_LOCALE
}

interface LocaleState {
  locale: AppLocale
  setLocale: (next: AppLocale) => void
}

export const useLocaleStore = create<LocaleState>((set) => ({
  locale: detectInitialLocale(),
  setLocale: (next) => {
    localStorage.setItem(STORAGE_KEY, next)
    set({ locale: next })
  },
}))

export function getLocale(): AppLocale {
  return useLocaleStore.getState().locale
}

export function tmdbLanguage(locale: AppLocale = getLocale()): string {
  return locale === 'it' ? 'it-IT' : 'en-US'
}

export function t(key: MessageKey, params?: InterpolationParams): string {
  return translate(getLocale(), key, params)
}