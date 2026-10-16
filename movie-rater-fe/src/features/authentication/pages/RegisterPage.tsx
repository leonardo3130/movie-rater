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
import { PasswordStrengthMeter } from '../components/PasswordStrengthMeter'
import { registerSchema, type RegisterFormValues } from '../schemas/register.schema'
import { register as registerApi } from '../../../api/endpoints/auth'
import { useAuthStore } from '../../../stores/auth-store'
import { useI18n } from '@src/i18n/use-i18n'
import { getApiErrorMessage } from '@src/api/errors'

export function RegisterPage() {
  const navigate = useNavigate()
  const setAuth = useAuthStore((s) => s.setAuth)
  const { t } = useI18n()
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  })

  const password = watch('password', '')

  const mutation = useMutation({
    mutationFn: registerApi,
    onSuccess: (data) => {
      setAuth(data.user, data.accessToken)
      navigate('/dashboard', { replace: true })
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('auth.register.registrationFailedGeneric')))
    },
  })

  const onSubmit = (values: RegisterFormValues) => {
    mutation.mutate({
      username: values.username,
      email: values.email,
      password: values.password,
    })
  }

  const pending = isSubmitting || mutation.isPending

  return (
    <AuthLayout
      title={t('auth.register.title')}
      subtitle={t('auth.register.subtitle')}
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="username">{t('auth.register.username')}</Label>
          <Input
            id="username"
            type="text"
            placeholder={t('auth.register.usernamePlaceholder')}
            autoComplete="username"
            aria-invalid={!!errors.username}
            {...register('username')}
          />
          {errors.username && (
            <p className="text-xs text-destructive">{errors.username.message}</p>
          )}
        </div>

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
              placeholder={t('auth.minCharsPlaceholder')}
              autoComplete="new-password"
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
          <PasswordStrengthMeter password={password} />
          {errors.password && (
            <p className="text-xs text-destructive">{errors.password.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirmPassword">{t('auth.confirmPassword')}</Label>
          <div className="relative">
            <Input
              id="confirmPassword"
              type={showConfirm ? 'text' : 'password'}
              placeholder={t('auth.repeatPasswordPlaceholder')}
              autoComplete="new-password"
              aria-invalid={!!errors.confirmPassword}
              className="pr-10"
              {...register('confirmPassword')}
            />
            <button
              type="button"
              onClick={() => setShowConfirm(!showConfirm)}
              className="absolute right-2 top-1/2 -translate-y-1/2 cursor-pointer text-muted-foreground hover:text-foreground"
              tabIndex={-1}
            >
              {showConfirm ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
            </button>
          </div>
          {errors.confirmPassword && (
            <p className="text-xs text-destructive">{errors.confirmPassword.message}</p>
          )}
        </div>

        <Button type="submit" className="w-full" disabled={pending}>
          {pending && <Loader2 className="size-4 animate-spin" />}
          {t('auth.register.createAccount')}
        </Button>
      </form>

      <p className="text-center text-sm text-muted-foreground">
        {t('auth.register.alreadyHaveAccount')}{' '}
        <Link
          to="/login"
          className="font-medium text-primary underline-offset-4 hover:underline"
        >
          {t('auth.signIn')}
        </Link>
      </p>
    </AuthLayout>
  )
}