'use client'

import { useState } from 'react'
import { useTranslations } from 'next-intl'
import { api, type ImportResultDto } from '@/lib/api-client'
import { VAFilePicker } from '@/components/va'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5066'

type Props = { open: boolean; partOpId: number; opNumber: string; onClose: () => void; onImported: () => void }

export function ImportDimensionsDialog({ open, partOpId, opNumber, onClose, onImported }: Props) {
  const t = useTranslations('parts.importDims')
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<ImportResultDto | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (!open) return null

  function close() {
    setFile(null); setError(null); setResult(null); setSubmitting(false)
    onClose()
  }

  async function onSubmit() {
    if (!file) { setError(t('errorNoFile')); return }
    setError(null)
    setSubmitting(true)
    const res = await api.operations.importDimensions(partOpId, file)
    setSubmitting(false)
    if (res.success && res.data) setResult(res.data)
    else setError(res.error ?? t('errorGeneric'))
  }

  function onCloseResult() {
    setFile(null); setError(null); setResult(null)
    onImported()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-lg">
        <CardHeader><CardTitle>{t('title', { op: opNumber })}</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">{t('description')}</p>

          {!result && (
            <>
              <a href={`${API_URL}/api/v1/operations/dimensions/import/template`} download="import-dimensions-template.xlsx" className="text-sm underline text-primary">
                {t('template')}
              </a>
              <VAFilePicker
                accept=".xlsx,.xls"
                label={t('fileLabel')}
                hint="Kéo thả file vào đây hoặc bấm để chọn (.xlsx)"
                onChange={files => setFile(files?.[0] ?? null)}
              />
              {error && <p className="text-sm text-destructive">{error}</p>}
              <div className="flex gap-2 pt-2">
                <Button onClick={onSubmit} disabled={submitting} className="flex-1">
                  {submitting ? t('submitting') : t('submit')}
                </Button>
                <Button type="button" variant="outline" onClick={close}>{t('cancel')}</Button>
              </div>
            </>
          )}

          {result && (
            <>
              <p className="text-sm font-medium">
                {t('result', { created: result.created, skipped: result.skipped })}
              </p>
              {result.errors.length > 0 && (
                <ul className="max-h-48 overflow-y-auto space-y-1 text-sm text-destructive">
                  {result.errors.map((e, i) => (
                    <li key={i}>{t('rowError', { row: e.rowNumber, message: e.message })}</li>
                  ))}
                </ul>
              )}
              <div className="flex gap-2 pt-2">
                <Button onClick={onCloseResult} className="flex-1">{t('close')}</Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
