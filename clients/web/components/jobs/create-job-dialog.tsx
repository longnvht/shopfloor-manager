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
  jobNumber: z.string().min(1, 'Bắt buộc').max(20),
  partRevId: z.string().min(1, 'Bắt buộc'),
  routingRevId: z.string().min(1, 'Bắt buộc'),
  runQty: z.string().optional(),
  shipBy: z.string().optional(),
})
type FormData = z.infer<typeof schema>

type Props = { open: boolean; onClose: () => void; onCreated: () => void }

export function CreateJobDialog({ open, onClose, onCreated }: Props) {
  const [error, setError] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  if (!open) return null

  async function onSubmit(data: FormData) {
    setError(null)
    const res = await api.jobs.create({
      jobNumber: data.jobNumber,
      partRevId: Number(data.partRevId),
      routingRevId: Number(data.routingRevId),
      runQty: data.runQty ? Number(data.runQty) : undefined,
      shipBy: data.shipBy || undefined,
    })
    if (res.success) { reset(); onClose(); onCreated() }
    else setError(res.error ?? 'Lỗi tạo job')
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Tạo Job mới</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label>Job Number</Label>
              <Input {...register('jobNumber')} placeholder="J2026-0001" />
              {errors.jobNumber && <p className="text-sm text-destructive">{errors.jobNumber.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Part Rev ID</Label>
                <Input {...register('partRevId')} type="number" placeholder="1" />
                {errors.partRevId && <p className="text-sm text-destructive">{errors.partRevId.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Routing Rev ID</Label>
                <Input {...register('routingRevId')} type="number" placeholder="1" />
                {errors.routingRevId && <p className="text-sm text-destructive">{errors.routingRevId.message}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Run Qty</Label>
                <Input {...register('runQty')} type="number" placeholder="10" />
              </div>
              <div className="space-y-1.5">
                <Label>Ship By</Label>
                <Input {...register('shipBy')} type="date" />
              </div>
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <div className="flex gap-2 pt-2">
              <Button type="submit" disabled={isSubmitting} className="flex-1">
                {isSubmitting ? 'Đang tạo...' : 'Tạo Job'}
              </Button>
              <Button type="button" variant="outline" onClick={() => { reset(); onClose() }}>Huỷ</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
