'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { api } from '@/lib/api-client'
import { useAuthStore } from '@/stores/auth.store'

const schema = z.object({
  userLogin: z.string().min(1, 'Vui lòng nhập tên đăng nhập'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
})
type FormData = z.infer<typeof schema>

export function LoginForm() {
  const router = useRouter()
  const login = useAuthStore((s) => s.login)
  const [error, setError] = useState<string | null>(null)

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  async function onSubmit(data: FormData) {
    setError(null)
    try {
      const res = await api.auth.login(data.userLogin, data.password)
      if (!res.success || !res.data) {
        setError(res.error ?? 'Đăng nhập thất bại')
        return
      }
      login(res.data.token, {
        id: res.data.userId,
        name: res.data.name,
        role: res.data.role,
        firstLogin: res.data.firstLogin,
      })
      router.push(res.data.firstLogin ? '/change-password' : '/dashboard')
    } catch {
      setError('Không thể kết nối đến máy chủ. Vui lòng thử lại.')
    }
  }

  return (
    <Card className="w-full max-w-sm">
      <CardHeader className="text-center">
        <CardTitle className="text-2xl">Shopfloor Manager</CardTitle>
        <CardDescription>Đăng nhập vào hệ thống</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="userLogin">Tên đăng nhập</Label>
            <Input
              id="userLogin"
              autoComplete="username"
              autoFocus
              {...register('userLogin')}
            />
            {errors.userLogin && (
              <p className="text-sm text-destructive">{errors.userLogin.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="password">Mật khẩu</Label>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              {...register('password')}
            />
            {errors.password && (
              <p className="text-sm text-destructive">{errors.password.message}</p>
            )}
          </div>

          {error && (
            <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </p>
          )}

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
