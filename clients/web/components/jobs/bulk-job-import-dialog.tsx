'use client'

import { useState } from 'react'
import { useTranslations } from 'next-intl'
import { api, type GlobalImportResultDto } from '@/lib/api-client'
import { va } from '@/lib/va-tokens'
import { VABtn, VACard, VAFilePicker } from '@/components/va'

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5066'

type Props = { open: boolean; onClose: () => void; onImported: () => void }

export function BulkJobImportDialog({ open, onClose, onImported }: Props) {
  const t = useTranslations('jobs.bulkImport')
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<GlobalImportResultDto | null>(null)
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
    const res = await api.jobs.importBatch(file)
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
      <VACard style={{ width: '100%', maxWidth: 560, maxHeight: '85vh', display: 'flex', flexDirection: 'column' }}>
        <div style={{ fontFamily: va.serif, fontSize: 18, fontWeight: 600, color: va.text, marginBottom: 4 }}>{t('title')}</div>
        <div className="va-scroll" style={{ overflow: 'auto', display: 'flex', flexDirection: 'column', gap: 12 }}>
          <p style={{ fontSize: 12.5, color: va.text2 }}>{t('description')}</p>

          {!result && (
            <>
              <a
                href={`${API_URL}/api/v1/jobs/import-batch/template`}
                download="import-jobs-template.xlsx"
                style={{ alignSelf: 'flex-start', fontSize: 12.5, color: va.primary, textDecoration: 'underline' }}
              >
                {t('template')}
              </a>

              <VAFilePicker
                accept=".xlsx,.xls"
                label={t('chooseFile')}
                hint="Kéo thả file vào đây hoặc bấm để chọn (.xlsx)"
                onChange={files => setFile(files?.[0] ?? null)}
              />

              {error && <p style={{ fontSize: 12.5, color: va.err }}>{error}</p>}

              <div style={{ display: 'flex', gap: 8, paddingTop: 4 }}>
                <VABtn kind="primary" onClick={onSubmit} disabled={submitting} style={{ flex: 1 }}>
                  {submitting ? t('submitting') : t('submit')}
                </VABtn>
                <VABtn kind="ghost" onClick={close}>{t('cancel')}</VABtn>
              </div>
            </>
          )}

          {result && (
            <>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, fontSize: 12.5 }}>
                <ResultRow label={t('result.partsCreated')} value={result.partsCreated} />
                <ResultRow label={t('result.partRevsCreated')} value={result.partRevsCreated} />
                <ResultRow label={t('result.opsCreated')} value={result.opsCreated} />
                <ResultRow label={t('result.opsUpdated')} value={result.opsUpdated} />
                <ResultRow label={t('result.jobsCreated')} value={result.jobsCreated} />
                <ResultRow label={t('result.jobsUpdated')} value={result.jobsUpdated} />
                <ResultRow label={t('result.productsCreated')} value={result.productsCreated} />
              </div>

              {result.errors.length > 0 && (
                <div className="va-scroll" style={{ maxHeight: 220, overflow: 'auto', border: `1px solid ${va.border}`, borderRadius: 8, padding: '6px 10px' }}>
                  <ul style={{ display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12, color: va.err, margin: 0, paddingLeft: 16 }}>
                    {result.errors.map((e, i) => (
                      <li key={i}>{t('rowError', { row: e.rowNumber, message: e.message })}</li>
                    ))}
                  </ul>
                </div>
              )}

              <div style={{ display: 'flex', gap: 8, paddingTop: 4 }}>
                <VABtn kind="primary" onClick={onCloseResult} style={{ flex: 1 }}>{t('close')}</VABtn>
              </div>
            </>
          )}
        </div>
      </VACard>
    </div>
  )
}

function ResultRow({ label, value }: { label: string; value: number }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '6px 10px', background: va.surface2, borderRadius: 6 }}>
      <span style={{ color: va.text2 }}>{label}</span>
      <span style={{ fontFamily: va.mono, fontWeight: 700, color: va.text }}>{value}</span>
    </div>
  )
}
