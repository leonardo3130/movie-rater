import { useEffect, useRef, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2, CheckCircle2, XCircle } from 'lucide-react'
import { toast } from 'sonner'

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { acceptInvitation } from '../../../api/endpoints/group.ts'
import { useGroupsStore } from '@/src/stores/groups-store.ts'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

type AcceptState =
  'loading' | 'success' | 'error'

export function AcceptInvitePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [state, setState] = useState<AcceptState>('loading')
  const [errorMessage, setErrorMessage] = useState('')
  const queryClient = useQueryClient();
  const setCurrentGroup = useGroupsStore(s => s.setCurrentGroup);

  const token = searchParams.get('token')

  const mutation = useMutation({
    mutationFn: acceptInvitation,
    onSuccess: (response) => {
      setState('success')
      toast.success(t('groups.accept.joinedToast'))
      queryClient.invalidateQueries({ queryKey: ['user-groups'] })
      setCurrentGroup(response.groupId);
      setTimeout(() => navigate('/dashboard', { replace: true }), 2000)
    },
    onError: (error: unknown) => {
      setState('error')
      const message = getApiErrorMessage(error, t('groups.accept.failedDefault'))
      setErrorMessage(message)
      toast.error(message)
    },
  })

  const hasMutated = useRef(false)

  useEffect(() => {
    if (!token) {
      setState('error')
      setErrorMessage(t('groups.accept.noToken'))
      return
    }

    if (hasMutated.current) return
    hasMutated.current = true

    mutation.mutate({ inviteToken: token })
  }, [token, mutation])

  return (
    <div className="mx-auto max-w-md px-4 pt-8 sm:pt-16">
      <Card>
        <CardHeader className="text-center">
          {state === 'loading' && (
            <div className="flex justify-center mb-4">
              <Loader2 className="size-10 animate-spin text-primary" />
            </div>
          )}
          {state === 'success' && (
            <div className="flex justify-center mb-4">
              <CheckCircle2 className="size-10 text-green-500" />
            </div>
          )}
          {state === 'error' && (
            <div className="flex justify-center mb-4">
              <XCircle className="size-10 text-destructive" />
            </div>
          )}

          <CardTitle>
            {state === 'loading' && t('groups.accept.acceptingTitle')}
            {state === 'success' && t('groups.accept.successTitle')}
            {state === 'error' && t('groups.accept.errorTitle')}
          </CardTitle>
          <CardDescription>
            {state === 'loading' && t('groups.accept.acceptingDescription')}
            {state === 'success' && t('groups.accept.successDescription')}
            {state === 'error' && errorMessage}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex justify-center">
          {state === 'error' && (
            <div className="flex gap-3">
              <Button variant="outline" onClick={() => navigate('/dashboard')}>
                {t('groups.accept.goToDashboard')}
              </Button>
              <Button onClick={() => navigate('/invite')}>
                {t('groups.accept.createNewInvitation')}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
