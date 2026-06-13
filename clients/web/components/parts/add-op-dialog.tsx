'use client'

import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslations } from 'next-intl'
import { api, type PartOpDto, type OpTypeDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const schema = z.object({
  opNumber: z.string().min(1).max(10),
  opTypeId: z.string().optional(),
  description: z.string().max(300).optional(),
  setupTime: z.string().optional(),
  prodTime: z.string().optional(),
})
type FormData = z.infer<typeof schema>

type Props = { open: boolean; routingRevId?: number; jobId?: number; onClose: () => void; onCreated: (op: PartOpDto) => void }

export function AddOpDialog({ open, routingRevId, jobId, onClose, onCreated }: Props) {
  const t = useTranslations('parts.addOp')
  const [error, setError] = useState<string | null>(null)
  const [opTypes, setOpTypes] = useState<OpTypeDto[]>([])
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  useEffect(() => {
    if (!open) return
    api.opTypes.list(true).then(res => { if (res.success && res.data) setOpTypes(res.data) })
  }, [open])

  if (!open) return null

  async function onSubmit(data: FormData) {
    setError(null)
    const res = await api.operations.create({
      routingRevId,
      jobId,
      opNumber: data.opNumber,
      opTypeId: data.opTypeId ? Number(data.opTypeId) : undefined,
      description: data.description || undefined,
      setupTime: data.setupTime ? Number(data.setupTime) : undefined,
      prodTime: data.prodTime ? Number(data.prodTime) : undefined,
    })
    if (res.success && res.data) { reset(); onClose(); onCreated(res.data) }
    else setError(res.error ?? t('errorGeneric'))
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-md">
        <CardHeader><CardTitle>{t('title')}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>{t('opNumber')}</Label>
                <Input {...register('opNumber')} placeholder="10" />
                {errors.opNumber && <p className="text-sm text-destructive">{t('errorRequired')}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>{t('opType')}</Label>
                <select
                  {...register('opTypeId')}
                  className="h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none focus-visible:border-ring"
                >
                  <option value="">{t('opTypeNone')}</option>
                  {opTypes.map(ot => <option key={ot.id} value={ot.id}>{ot.code}{ot.name ? ` — ${ot.name}` : ''}</option>)}
                </select>
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>{t('description')}</Label>
              <Input {...register('description')} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>{t('setupTime')}</Label>
                <Input {...register('setupTime')} type="number" step="0.01" min="0" />
              </div>
              <div className="space-y-1.5">
                <Label>{t('prodTime')}</Label>
                <Input {...register('prodTime')} type="number" step="0.01" min="0" />
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
