import { useEffect, useState } from 'react'

// Chromium-only event, not yet in lib.dom
interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

declare global {
  interface WindowEventMap {
    beforeinstallprompt: BeforeInstallPromptEvent
  }
  interface Navigator {
    // Set by iOS Safari when launched from the home screen
    standalone?: boolean
  }
}

function isStandalone() {
  return window.matchMedia('(display-mode: standalone)').matches || navigator.standalone === true
}

function isIos() {
  // iPadOS reports itself as macOS, so fall back to touch support
  return (
    /iphone|ipad|ipod/i.test(navigator.userAgent) ||
    (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
  )
}

/**
 * Exposes how the app can be installed on the current device:
 * - `prompt`: Chromium browsers (Android, desktop) fired `beforeinstallprompt`, so `install()` opens the native dialog
 * - `ios`: Safari has no install API, so the user must use Share → Add to Home Screen
 * - `null`: already installed or the browser does not support installation
 */
export function useInstallPrompt() {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null)
  const [installed, setInstalled] = useState(isStandalone)

  useEffect(() => {
    const onBeforeInstallPrompt = (event: BeforeInstallPromptEvent) => {
      event.preventDefault()
      setDeferredPrompt(event)
    }
    const onInstalled = () => {
      setDeferredPrompt(null)
      setInstalled(true)
    }

    window.addEventListener('beforeinstallprompt', onBeforeInstallPrompt)
    window.addEventListener('appinstalled', onInstalled)
    return () => {
      window.removeEventListener('beforeinstallprompt', onBeforeInstallPrompt)
      window.removeEventListener('appinstalled', onInstalled)
    }
  }, [])

  const install = async () => {
    if (!deferredPrompt) return
    await deferredPrompt.prompt()
    // The event can only be used once, whatever the user chose
    setDeferredPrompt(null)
  }

  const mode = installed ? null : deferredPrompt ? 'prompt' : isIos() ? 'ios' : null

  return { mode, install } as const
}
