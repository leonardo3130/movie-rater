import { useEffect } from 'react'
import { toast } from 'sonner'
import { useRegisterSW } from 'virtual:pwa-register/react'
import { useI18n } from '@src/i18n/use-i18n'

// How often an open tab (or installed app left in the background) checks for a new deploy
const UPDATE_CHECK_INTERVAL_MS = 60 * 60 * 1000

export function PwaUpdatePrompt() {
  const { t } = useI18n()
  const {
    needRefresh: [needRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    onRegisteredSW(_swUrl, registration) {
      if (!registration) return
      setInterval(() => {
        if (navigator.onLine) registration.update()
      }, UPDATE_CHECK_INTERVAL_MS)
    },
  })

  useEffect(() => {
    if (!needRefresh) return
    toast(t('pwa.updateAvailable'), {
      id: 'pwa-update',
      duration: Infinity,
      action: {
        label: t('pwa.reload'),
        onClick: () => updateServiceWorker(),
      },
    })
  }, [needRefresh, updateServiceWorker, t])

  return null
}
