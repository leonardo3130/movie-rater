import { useState } from 'react'
import { Download, Share, SquarePlus } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem } from '@/components/ui/sidebar'
import { useI18n } from '@src/i18n/use-i18n'
import { useInstallPrompt } from '../hooks/use-install-prompt'

export function InstallAppButton() {
  const { mode, install } = useInstallPrompt()
  const [iosHelpOpen, setIosHelpOpen] = useState(false)
  const { t } = useI18n()

  if (!mode) return null

  const label = t('pwa.install')

  return (
    <>
      <SidebarMenu>
        <SidebarMenuItem>
          <SidebarMenuButton
            tooltip={label}
            onClick={() => (mode === 'prompt' ? install() : setIosHelpOpen(true))}
            className="text-muted-foreground hover:text-sidebar-foreground hover:bg-sidebar-accent"
          >
            <Download className="size-4" />
            <span className="text-sm font-medium group-data-[collapsible=icon]:hidden">{label}</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>

      <Dialog open={iosHelpOpen} onOpenChange={setIosHelpOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('pwa.iosTitle')}</DialogTitle>
            <DialogDescription>{t('pwa.iosDescription')}</DialogDescription>
          </DialogHeader>
          <ol className="space-y-3">
            <li className="flex items-center gap-3">
              <Share className="size-5 shrink-0 text-primary" />
              <span>{t('pwa.iosStepShare')}</span>
            </li>
            <li className="flex items-center gap-3">
              <SquarePlus className="size-5 shrink-0 text-primary" />
              <span>{t('pwa.iosStepAdd')}</span>
            </li>
          </ol>
        </DialogContent>
      </Dialog>
    </>
  )
}
