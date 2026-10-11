import { useQueryClient } from '@tanstack/react-query'
import { Languages } from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useI18n } from '@src/i18n/use-i18n'
import { APP_LOCALES } from '@src/i18n/lib'
import type { AppLocale } from '@src/i18n/lib'
import { useLocaleStore } from '@src/stores/locale-store'

const LOCALE_LABELS: Record<AppLocale, string> = {
  en: 'English',
  it: 'Italiano',
}

export function LanguageSwitcher() {
  const { locale, t } = useI18n()
  const setLocale = useLocaleStore((s) => s.setLocale)
  const queryClient = useQueryClient()

  return (
    <Select
      value={locale}
      onValueChange={(next) => {
        setLocale(next as AppLocale)
        queryClient.invalidateQueries()
      }}
    >
      <SelectTrigger
        aria-label={t('common.language')}
        title={t('common.language')}
        size="sm"
        className="gap-1.5"
      >
        <Languages className="size-4 text-muted-foreground" />
        <SelectValue>{LOCALE_LABELS[locale]}</SelectValue>
      </SelectTrigger>
      <SelectContent align="end">
        <SelectGroup>
          <SelectLabel>{t('common.language')}</SelectLabel>
          {(Object.keys(APP_LOCALES) as AppLocale[]).map((option) => (
            <SelectItem key={option} value={option}>
              {LOCALE_LABELS[option]}
            </SelectItem>
          ))}
        </SelectGroup>
      </SelectContent>
    </Select>
  )
}