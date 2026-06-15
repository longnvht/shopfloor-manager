'use client'

import { useState } from 'react'
import { useTranslations } from 'next-intl'
import { api, type FileTypeDto, type UploadDocBody } from '@/lib/api-client'
import {
  buildBatchRows, mergeResolveResults, applyClientChecks, toResolveBatchItem,
  segmentToken, describeMatch, type BatchRow, type BatchStatus,
} from '@/lib/bulk-upload-parser'
import { formatBytes } from '@/lib/doc-format'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { VABtn, VABadge } from '@/components/va'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Props = { open: boolean; onClose: () => void; onDone: () => void }

const STATUS_KIND: Record<BatchStatus, VaBadgeKind> = {
  Ready: 'ok',
  Duplicate: 'warn',
  Invalid: 'err',
  SegmentIncomplete: 'warn',
  Uploading: 'primary',
  Success: 'ok',
  Error: 'err',
}

export function BulkUploadDialog({ open, onClose, onDone }: Props) {
  const t = useTranslations('documents.bulkUpload')
  const [rows, setRows] = useState<BatchRow[]>([])
  const [resolving, setResolving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fileTypes, setFileTypes] = useState<FileTypeDto[] | null>(null)

  if (!open) return null

  function close() {
    setRows([]); setError(null); setResolving(false); setUploading(false)
    onClose()
  }

  async function ensureFileTypes(): Promise<FileTypeDto[]> {
    if (fileTypes) return fileTypes
    const res = await api.techDocuments.fileTypes()
    const list = res.success && res.data ? res.data : []
    setFileTypes(list)
    return list
  }

  async function handleFilesSelected(fileList: FileList | null) {
    if (!fileList || fileList.length === 0) return
    setError(null)
    setResolving(true)
    try {
      const files = Array.from(fileList)
      const types = await ensureFileTypes()
      const built = buildBatchRows(files, types)
      const items = built.map(r => toResolveBatchItem(r.parsed, r.file.size))
      const res = await api.techDocuments.resolveBatch(items)
      if (!res.success || !res.data) {
        setError(res.error ?? t('errorResolve'))
        setRows(built)
        return
      }
      const merged = mergeResolveResults(built, res.data)
      setRows(applyClientChecks(merged))
    } finally {
      setResolving(false)
    }
  }

  const readyCount = rows.filter(r => r.status === 'Ready').length

  async function handleUploadAll() {
    setUploading(true)
    for (let i = 0; i < rows.length; i++) {
      if (rows[i].status !== 'Ready') continue
      setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Uploading' } : r))

      const row = rows[i]
      const resolve = row.resolve
      if (!resolve || resolve.fileTypeId == null) {
        setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Error' } : r))
        continue
      }

      const body: UploadDocBody = {
        fileTypeId: resolve.fileTypeId,
        fileName: row.fileName,
        partRevId: resolve.partRevId,
        partOpId: resolve.partOpId,
        jobId: resolve.jobId,
        description: null,
        revision: null,
        code: null,
        segment: segmentToken(row.parsed),
        machineType: null,
        fileSizeBytes: row.file.size,
      }

      try {
        const res = await api.techDocuments.create(body)
        if (!res.success || !res.data) {
          setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Error', errorMessage: res.error ?? 'create failed' } : r))
          continue
        }
        const uploadRes = await fetch(res.data.uploadUrl, { method: 'PUT', body: row.file })
        if (!uploadRes.ok) {
          setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Error', errorMessage: `Upload failed (${uploadRes.status})` } : r))
          continue
        }
        setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Success' } : r))
      } catch (err) {
        setRows(prev => prev.map((r, idx) => idx === i ? { ...r, status: 'Error', errorMessage: err instanceof Error ? err.message : String(err) } : r))
      }
    }
    setUploading(false)
    onDone()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-4xl max-h-[85vh] flex flex-col">
        <CardHeader><CardTitle>{t('title')}</CardTitle></CardHeader>
        <CardContent className="space-y-4 overflow-y-auto flex-1">
          <p className="text-sm text-muted-foreground">{t('hint')}</p>

          <div className="flex gap-2">
            <label className="inline-flex">
              <input
                type="file" multiple className="hidden"
                onChange={e => handleFilesSelected(e.target.files)}
              />
              <span className="cursor-pointer"><VABtn kind="ghost">{t('selectFiles')}</VABtn></span>
            </label>
            <label className="inline-flex">
              <input
                type="file" multiple className="hidden"
                ref={el => {
                  if (el) (el as HTMLInputElement & { webkitdirectory: boolean }).webkitdirectory = true
                }}
                onChange={e => handleFilesSelected(e.target.files)}
              />
              <span className="cursor-pointer"><VABtn kind="ghost">{t('selectFolder')}</VABtn></span>
            </label>
          </div>

          {resolving && <p className="text-sm" style={{ color: va.text2 }}>{t('resolving')}</p>}
          {error && <p className="text-sm text-destructive">{error}</p>}

          {rows.length > 0 && (
            <div className="va-scroll" style={{ overflow: 'auto', maxHeight: 360, border: `1px solid ${va.border}`, borderRadius: 8 }}>
              <table style={{ width: '100%', fontSize: 12.5, borderCollapse: 'collapse' }}>
                <thead>
                  <tr>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.file')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.type')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.detected')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'right', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.size')}</th>
                    <th style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}` }}>{t('table.headers.status')}</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r, i) => (
                    <tr key={i}>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5 }}>{r.fileName}</td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5 }}>{r.parsed.fileTypeCode ?? '—'}</td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontSize: 11.5, color: va.text2 }}>
                        {r.resolve ? describeMatch(r.resolve) : '—'}
                      </td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5, textAlign: 'right', color: va.text2 }}>
                        {formatBytes(r.file.size)}
                      </td>
                      <td style={{ padding: '7px 10px', borderBottom: `1px solid ${va.separator}` }}>
                        <VABadge kind={STATUS_KIND[r.status]}>{t(`status.${r.status}`)}</VABadge>
                        {r.reason && <div style={{ fontSize: 10.5, color: va.text3, marginTop: 2 }}>{t(`reason.${r.reason}`)}</div>}
                        {r.errorMessage && <div style={{ fontSize: 10.5, color: va.text3, marginTop: 2 }}>{t('uploadErrorPrefix')}{r.errorMessage}</div>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {rows.length > 0 && (
            <p className="text-sm" style={{ color: va.text2 }}>{t('readyCount', { count: readyCount, total: rows.length })}</p>
          )}
        </CardContent>
        <div className="flex gap-2 justify-end p-4 pt-0">
          <VABtn kind="ghost" onClick={close}>{rows.some(r => r.status === 'Success') ? t('close') : t('cancel')}</VABtn>
          <VABtn kind="primary" disabled={readyCount === 0 || uploading || resolving} onClick={handleUploadAll}>
            {t('uploadButton', { count: readyCount })}
          </VABtn>
        </div>
      </Card>
    </div>
  )
}
