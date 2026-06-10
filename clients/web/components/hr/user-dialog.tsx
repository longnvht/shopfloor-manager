'use client'

import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { api, type UserListDto, type PositionDto, type UserTypeDto, type WorkStatusDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const selectClass = 'h-8 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50'

const schema = z.object({
  userLogin: z.string().optional(),
  password: z.string().optional(),
  name: z.string().min(1, 'Bắt buộc').max(100),
  email: z.union([z.string().email('Email không hợp lệ'), z.literal('')]).optional(),
  sex: z.string().optional(),
  roleId: z.string().optional(),
  userTypeId: z.string().optional(),
  positionId: z.string().optional(),
  workStatusId: z.string().optional(),
  isActive: z.boolean().optional(),
})
type FormData = z.infer<typeof schema>

type Props = { open: boolean; user: UserListDto | null; onClose: () => void; onSaved: () => void }

export function UserDialog({ open, user, onClose, onSaved }: Props) {
  const [error, setError] = useState<string | null>(null)
  const [roles, setRoles] = useState<{ id: number; name: string }[]>([])
  const [userTypes, setUserTypes] = useState<UserTypeDto[]>([])
  const [positions, setPositions] = useState<PositionDto[]>([])
  const [workStatuses, setWorkStatuses] = useState<WorkStatusDto[]>([])

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  useEffect(() => {
    if (!open) return
    Promise.all([api.lookups.roles(), api.lookups.userTypes(), api.lookups.positions(), api.lookups.workStatuses()])
      .then(([r, ut, p, ws]) => {
        if (r.success && r.data) setRoles(r.data)
        if (ut.success && ut.data) setUserTypes(ut.data)
        if (p.success && p.data) setPositions(p.data)
        if (ws.success && ws.data) setWorkStatuses(ws.data)
      })
    setError(null)
    reset({
      userLogin: user?.userLogin ?? '',
      password: '',
      name: user?.name ?? '',
      email: user?.email ?? '',
      sex: user?.sex ?? '',
      roleId: user?.roleId ? String(user.roleId) : '',
      userTypeId: user?.userTypeId ? String(user.userTypeId) : '',
      positionId: user?.positionId ? String(user.positionId) : '',
      workStatusId: user?.workStatusId ? String(user.workStatusId) : '',
      isActive: user?.isActive ?? true,
    })
  }, [open, user, reset])

  if (!open) return null

  async function onSubmit(data: FormData) {
    setError(null)
    const toId = (v?: string) => (v ? Number(v) : undefined)

    if (!user) {
      if (!data.userLogin?.trim()) { setError('Tên đăng nhập bắt buộc'); return }
      if (!data.password || data.password.length < 6) { setError('Mật khẩu tối thiểu 6 ký tự'); return }
      const res = await api.users.create({
        userLogin: data.userLogin.trim(),
        password: data.password,
        name: data.name,
        email: data.email || undefined,
        roleId: toId(data.roleId),
        userTypeId: toId(data.userTypeId),
        positionId: toId(data.positionId),
        workStatusId: toId(data.workStatusId),
      })
      if (res.success) { reset(); onClose(); onSaved() }
      else setError(res.error ?? 'Lỗi tạo tài khoản')
    } else {
      const res = await api.users.update(user.id, {
        name: data.name,
        email: data.email || undefined,
        sex: data.sex || undefined,
        roleId: toId(data.roleId),
        userTypeId: toId(data.userTypeId),
        positionId: toId(data.positionId),
        workStatusId: toId(data.workStatusId),
        isActive: data.isActive ?? true,
      })
      if (res.success) { reset(); onClose(); onSaved() }
      else setError(res.error ?? 'Lỗi cập nhật tài khoản')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <CardHeader><CardTitle>{user ? `Sửa tài khoản — ${user.name}` : 'Tạo tài khoản mới'}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Tên đăng nhập</Label>
                <Input {...register('userLogin')} placeholder="nguyenvana" disabled={!!user} />
              </div>
              <div className="space-y-1.5">
                <Label>{user ? 'Mật khẩu' : 'Mật khẩu *'}</Label>
                <Input {...register('password')} type="password" placeholder={user ? '(không đổi)' : 'tối thiểu 6 ký tự'} disabled={!!user} />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Họ tên *</Label>
              <Input {...register('name')} placeholder="Nguyễn Văn A" />
              {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Email</Label>
                <Input {...register('email')} type="email" placeholder="email@example.com" />
                {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Giới tính</Label>
                <select {...register('sex')} className={selectClass}>
                  <option value="">—</option>
                  <option value="Nam">Nam</option>
                  <option value="Nữ">Nữ</option>
                </select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Role (PDM)</Label>
                <select {...register('roleId')} className={selectClass}>
                  <option value="">—</option>
                  {roles.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                </select>
              </div>
              <div className="space-y-1.5">
                <Label>User Type</Label>
                <select {...register('userTypeId')} className={selectClass}>
                  <option value="">—</option>
                  {userTypes.map(t => <option key={t.id} value={t.id}>{t.typeName}</option>)}
                </select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Chức vụ</Label>
                <select {...register('positionId')} className={selectClass}>
                  <option value="">—</option>
                  {positions.map(p => <option key={p.id} value={p.id}>{p.code}</option>)}
                </select>
              </div>
              <div className="space-y-1.5">
                <Label>Trạng thái làm việc</Label>
                <select {...register('workStatusId')} className={selectClass}>
                  <option value="">—</option>
                  {workStatuses.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
                </select>
              </div>
            </div>
            {user && (
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" {...register('isActive')} className="h-4 w-4" />
                Tài khoản đang hoạt động
              </label>
            )}
            {error && <p className="text-sm text-destructive">{error}</p>}
            <div className="flex gap-2 pt-2">
              <Button type="submit" disabled={isSubmitting} className="flex-1">
                {isSubmitting ? 'Đang lưu...' : user ? 'Lưu thay đổi' : 'Tạo tài khoản'}
              </Button>
              <Button type="button" variant="outline" onClick={() => { reset(); onClose() }}>Huỷ</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
