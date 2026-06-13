'use client'

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslations } from 'next-intl'
import { api, type RoutingRevDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const schema = z.object({
  revCode: z.string().min(1).max(10),
  changeNote: z.string().max(300).optional(),
})
type FormData = z.infer<typeof schema>

type Props = { open: boolean; routingId: number; onClose: () => void; onCreated: (rev: RoutingRevDto) => void }

export function AddRoutingRevDialog({ open, routingId, onClose, onCreated }: Props) {
  const t = useTranslations('parts.addRoutingRev')
  const [error, setError] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  })

  if (!open) return null

  async function onSubmit(data: FormData) {
    setError(null)
    const res = await api.parts.addRoutingRev({ routingId, revCode: data.revCode, changeNote: data.changeNote || undefined })
    if (res.success && res.data) { reset(); onClose(); onCreated(res.data) }
    else setError(res.error ?? t('errorGeneric'))
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-md">
        <CardHeader><CardTitle>{t('title')}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label>{t('revCode')}</Label>
              <Input {...register('revCode')} placeholder="R2" className="w-24" />
              {errors.revCode && <p className="text-sm text-destructive">{t('errorRequired')}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>{t('changeNote')}</Label>
              <Input {...register('changeNote')} />
              <p className="text-xs text-muted-foreground">{t('hint')}</p>
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
