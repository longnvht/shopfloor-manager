'use client'

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslations } from 'next-intl'
import { api } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const schema = z.object({
  jobNumber: z.string().min(1).max(20),
  partRevId: z.string().min(1),
  routingRevId: z.string().min(1),
  runQty: z.string().optional(),
  shipBy: z.string().optional(),
})
type FormData = z.infer<typeof schema>

type Props = { open: boolean; onClose: () => void; onCreated: () => void }

export function CreateJobDialog({ open, onClose, onCreated }: Props) {
  const t = useTranslations('jobs.createDialog')
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
    else setError(res.error ?? t('errorGeneric'))
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>{t('title')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label>{t('jobNumber')}</Label>
              <Input {...register('jobNumber')} placeholder="J2026-0001" />
              {errors.jobNumber && <p className="text-sm text-destructive">{t('errorRequired')}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>{t('partRevId')}</Label>
                <Input {...register('partRevId')} type="number" placeholder="1" />
                {errors.partRevId && <p className="text-sm text-destructive">{t('errorRequired')}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>{t('routingRevId')}</Label>
                <Input {...register('routingRevId')} type="number" placeholder="1" />
                {errors.routingRevId && <p className="text-sm text-destructive">{t('errorRequired')}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>{t('runQty')}</Label>
                <Input {...register('runQty')} type="number" placeholder="10" />
              </div>
              <div className="space-y-1.5">
                <Label>{t('shipBy')}</Label>
                <Input {...register('shipBy')} type="date" />
              </div>
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <div className="flex gap-2 pt-2">
              <Button type="submit" disabled={isSubmitting} className="flex-1">
                {isSubmitting ? t('submitting') : t('submit')}
              </Button>
              <Button type="button" variant="outline" onClick={() => { reset(); onClose() }}>{t('cancel')}</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
