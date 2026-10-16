import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Link, useNavigate } from 'react-router'
import { useMutation } from '@tanstack/react-query'
import { Eye, EyeOff, Loader2 } from 'lucide-react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { AuthLayout } from '../components/AuthLayout'
import { loginSchema, type LoginFormValues } from '../schemas/login.schema'
import { login as loginApi, forgotPassword as forgotPasswordApi } from '../../../api/endpoints/auth'
import { useAuthStore } from '../../../stores/auth-store'
import { useI18n } from '@src/i18n/use-i18n'
import { getApiErrorMessage } from '@src/api/errors'

export function LoginPage() {
  const navigate = useNavigate()
  const setAuth = useAuthStore((s) => s.setAuth)
  const { t } = useI18n()
  const [showPassword, setShowPassword] = useState(false)

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  })

  const mutation = useMutation({
    mutationFn: loginApi,
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken)
      navigate('/dashboard', { replace: true })
    },
    onError: () => {
      toast.error(t('auth.login.invalidCredentials'))
    },
  })

  const forgotPasswordMutation = useMutation({
    mutationFn: forgotPasswordApi,
    onSuccess: () => {
      toast.success(t('auth.login.resetLinkSent'))
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('auth.login.resetLinkFailed')))
    },
  })

  const onSubmit = (values: LoginFormValues) => {
    mutation.mutate(values)
  }

  const pending = isSubmitting || mutation.isPending

  const onForgotPassword = () => {
    const email = watch('email')
    if (!email || email.trim() === '') {
      toast.error(t('auth.login.enterEmailFirst'))
      return
    }
    forgotPasswordMutation.mutate({ email })
  }

  return (
    <AuthLayout
      title={t('auth.login.title')}
      subtitle={t('auth.login.subtitle')}
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="email">{t('auth.email')}</Label>
          <Input
            id="email"
            type="email"
            placeholder={t('auth.emailPlaceholder')}
            autoComplete="email"
            aria-invalid={!!errors.email}
            {...register('email')}
          />
          {errors.email && (
            <p className="text-xs text-destructive">{errors.email.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">{t('auth.password')}</Label>
          <div className="relative">
            <Input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder={t('auth.passwordPlaceholder')}
              autoComplete="current-password"
              aria-invalid={!!errors.password}
              className="pr-10"
              {...register('password')}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-2 top-1/2 -translate-y-1/2 cursor-pointer text-muted-foreground hover:text-foreground"
              tabIndex={-1}
            >
              {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
            </button>
          </div>
          {errors.password && (
            <p className="text-xs text-destructive">{errors.password.message}</p>
          )}
          <Button
            type="button"
            variant="link"
            size="sm"
            className="h-auto px-0 font-normal"
            onClick={onForgotPassword}
            disabled={forgotPasswordMutation.isPending}
          >
            {forgotPasswordMutation.isPending && <Loader2 className="size-3 animate-spin" />}
            {t('auth.login.forgotPassword')}
          </Button>
        </div>

        <Button type="submit" className="w-full" disabled={pending}>
          {pending && <Loader2 className="size-4 animate-spin" />}
          {t('auth.signIn')}
        </Button>
      </form>

      <p className="text-center text-sm text-muted-foreground">
        {t('auth.login.noAccount')}{' '}
        <Link
          to="/register"
          className="font-medium text-primary underline-offset-4 hover:underline"
        >
          {t('auth.login.createOne')}
        </Link>
      </p>
    </AuthLayout>
  )
}