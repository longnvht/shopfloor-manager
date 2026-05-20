'use client'

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { api } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const schema = z.object({
  partNumber: z.string().min(1, 'Bắt buộc').max(20),
  description: z.string().min(1, 'Bắt buộc').max(300),
  revCode: z.string().min(1, 'Bắt buộc').max(10),
})
type FormData = z.infer<typeof schema>

type Props = { open: boolean; onClose: () => void; onCreated: () => void }

export function CreatePartDialog({ open, onClose, onCreated }: Props) {
  const [error, setError] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { revCode: 'A' },
  })

  if (!open) return null

  async function onSubmit(data: FormData) {
    setError(null)
    const res = await api.parts.create(data)
    if (res.success) { reset(); onClose(); onCreated() }
    else setError(res.error ?? 'Lỗi tạo Part')
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-md">
        <CardHeader><CardTitle>Thêm Part mới</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Part Number</Label>
              <Input {...register('partNumber')} placeholder="SHAFT-50H6" />
              {errors.partNumber && <p className="text-sm text-destructive">{errors.partNumber.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Mô tả</Label>
              <Input {...register('description')} placeholder="Trục CNC Ø50h6" />
              {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Revision đầu tiên</Label>
              <Input {...register('revCode')} placeholder="A" className="w-24" />
              <p className="text-xs text-muted-foreground">
                Tự động tạo Routing Standard R1 cho revision này.
              </p>
              {errors.revCode && <p className="text-sm text-destructive">{errors.revCode.message}</p>}
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <div className="flex gap-2 pt-2">
              <Button type="submit" disabled={isSubmitting} className="flex-1">
                {isSubmitting ? 'Đang tạo...' : 'Tạo Part'}
              </Button>
              <Button type="button" variant="outline" onClick={() => { reset(); onClose() }}>Huỷ</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
