import { en } from './locales/en'
import { it } from './locales/it'
import type { PluralMessage } from './plural'
import { selectPluralForm } from './plural'

export type AppLocale = 'en' | 'it'
export type Messages = typeof en

export const APP_LOCALES: Record<AppLocale, Messages> = { en, it }

export interface InterpolationParams {
  [name: string]: string | number
}

type KeyPaths<T> = {
  [K in keyof T & string]: T[K] extends string | PluralMessage
    ? K
    : `${K}.${KeyPaths<T[K]>}`
}[keyof T & string]

export type MessageKey = KeyPaths<Messages>

function lookup(dict: Messages, key: string): string | PluralMessage | undefined {
  let node: unknown = dict
  for (const part of key.split('.')) {
    if (node == null || typeof node !== 'object' || !(part in node)) return undefined
    node = (node as Record<string, unknown>)[part]
  }
  return node as string | PluralMessage | undefined
}

function interpolate(template: string, params?: InterpolationParams): string {
  return template.replace(/\{(\w+)\}/g, (match, name: string) =>
    params && name in params ? String(params[name]) : match,
  )
}

export function translate(
  locale: AppLocale,
  key: MessageKey,
  params?: InterpolationParams,
): string {
  const entry = lookup(APP_LOCALES[locale], key)
  if (entry == null) return key
  if (typeof entry === 'object') {
    const count = params && typeof params.count === 'number' ? params.count : 1
    const form = selectPluralForm(locale, count)
    return interpolate(entry[form] ?? entry.other, params)
  }
  return interpolate(entry, params)
}