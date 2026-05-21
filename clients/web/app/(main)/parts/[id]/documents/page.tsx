'use client'

import { useState, useEffect, useCallback } from 'react'
import { useParams, useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

type TechDocDto = {
  id: number
  fileTypeCode: string; fileTypeName: string
  partRevId: number | null; partOpId: number | null; jobId: number | null
  description: string | null; revision: string | null
  code: string | null; segment: string | null; machineType: string | null
  status: string; createdByName: string; createdAt: string
}

type FileTypeDto = { id: number; code: string; name: string; isPartNumber: boolean; isOpNumber: boolean; isJobNumber: boolean; sortOrder: number }

const STATUS_STYLE: Record<string, string> = {
  Pending:  'bg-yellow-100 text-yellow-700',
  Approved: 'bg-green-100 text-green-700',
  Rejected: 'bg-red-100 text-red-700',
}

const authHeader = () => ({ Authorization: `Bearer ${localStorage.getItem('auth-token')}` })
const apiBase = process.env.NEXT_PUBLIC_API_URL

export default function PartDocumentsPage() {
  const { id } = useParams<{ id: string }>()
  const searchParams = useSearchParams()
  // opId + opNumber set → OP-level docs; absent → Part-level (DRW/CAD) docs
  const opId     = searchParams.get('opId')
  const opNumber = searchParams.get('opNumber')
  const partRevId = searchParams.get('partRevId')
  const partNumber = searchParams.get('partNumber') ?? `Part #${id}`
  const revCode    = searchParams.get('revCode') ?? ''

  const [docs, setDocs]           = useState<TechDocDto[]>([])
  const [fileTypes, setFileTypes] = useState<FileTypeDto[]>([])
  const [loading, setLoading]     = useState(true)
  const [uploading, setUploading] = useState(false)
  const [uploadForm, setUploadForm] = useState<{ fileTypeId: string; file: File | null; revision: string; description: string; machineType: string } | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    const params = new URLSearchParams()
    if (opId) params.set('partOpId', opId)
    else if (partRevId) params.set('partRevId', partRevId)

    const [docsRes, ftRes] = await Promise.all([
      fetch(`${apiBase}/api/v1/tech-documents?${params}`, { headers: authHeader() }),
      fetch(`${apiBase}/api/v1/op-types`, { headers: authHeader() })  // reuse; actual file-types via lookups
    ])
    const docsData = await docsRes.json()
    if (docsData.success) setDocs(docsData.data ?? [])

    // Load file types from seed (filtered by context)
    const ftRes2 = await fetch(`${apiBase}/api/v1/tech-documents/file-types`, { headers: authHeader() }).catch(() => null)
    if (ftRes2?.ok) {
      const ftData = await ftRes2.json()
      if (ftData.success) setFileTypes(ftData.data ?? [])
    }

    setLoading(false)
  }, [opId, partRevId])

  useEffect(() => { load() }, [load])

  async function getDownloadUrl(docId: number) {
    const res = await fetch(`${apiBase}/api/v1/tech-documents/${docId}/download-url`, { headers: authHeader() })
    const data = await res.json()
    if (data.success && data.data) window.open(data.data, '_blank')
  }

  async function handleUpload() {
    if (!uploadForm?.file || !uploadForm.fileTypeId) return
    setUploading(true)
    try {
      // Step 1: Request pre-signed URL
      const body = {
        fileTypeId:  parseInt(uploadForm.fileTypeId),
        fileName:    uploadForm.file.name,
        partRevId:   opId ? null : (partRevId ? parseInt(partRevId) : null),
        partOpId:    opId ? parseInt(opId) : null,
        jobId:       null,
        description: uploadForm.description || null,
        revision:    uploadForm.revision || null,
        machineType: uploadForm.machineType || null,
      }
      const reqRes = await fetch(`${apiBase}/api/v1/tech-documents`, {
        method: 'POST', headers: { ...authHeader(), 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      })
      const reqData = await reqRes.json()
      if (!reqData.success) { alert(reqData.error); setUploading(false); return }

      // Step 2: Upload to MinIO pre-signed URL
      await fetch(reqData.data.uploadUrl, { method: 'PUT', body: uploadForm.file })

      setUploadForm(null)
      load()
    } finally {
      setUploading(false)
    }
  }

  const backHref = `/parts/${id}`
  const pageTitle = opId
    ? `Tài liệu — OP ${opNumber ?? opId}`
    : `Tài liệu bản vẽ — ${partNumber} Rev ${revCode}`

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link href={backHref} className="text-muted-foreground hover:text-foreground">← {partNumber}</Link>
          <span className="text-muted-foreground">/</span>
          <h1 className="text-xl font-semibold">{pageTitle}</h1>
        </div>
        <Button size="sm" onClick={() => setUploadForm({ fileTypeId: '', file: null, revision: '', description: '', machineType: '' })}>
          + Upload
        </Button>
      </div>

      {/* Upload form */}
      {uploadForm && (
        <Card>
          <CardContent className="pt-4 space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-sm font-medium">Loại file</label>
                <select
                  className="w-full mt-1 rounded-md border px-3 py-2 text-sm"
                  value={uploadForm.fileTypeId}
                  onChange={e => setUploadForm(f => f && ({ ...f, fileTypeId: e.target.value }))}>
                  <option value="">— Chọn loại —</option>
                  {fileTypes.map(ft => (
                    <option key={ft.id} value={ft.id}>{ft.code} — {ft.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-sm font-medium">Revision</label>
                <input className="w-full mt-1 rounded-md border px-3 py-2 text-sm"
                  placeholder="A, B, Rev.01..." value={uploadForm.revision}
                  onChange={e => setUploadForm(f => f && ({ ...f, revision: e.target.value }))} />
              </div>
              <div className="col-span-2">
                <label className="text-sm font-medium">File</label>
                <input type="file" className="w-full mt-1 text-sm"
                  onChange={e => setUploadForm(f => f && ({ ...f, file: e.target.files?.[0] ?? null }))} />
              </div>
              <div className="col-span-2">
                <label className="text-sm font-medium">Mô tả</label>
                <input className="w-full mt-1 rounded-md border px-3 py-2 text-sm"
                  value={uploadForm.description}
                  onChange={e => setUploadForm(f => f && ({ ...f, description: e.target.value }))} />
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" size="sm" onClick={() => setUploadForm(null)}>Huỷ</Button>
              <Button size="sm" disabled={uploading || !uploadForm.file || !uploadForm.fileTypeId} onClick={handleUpload}>
                {uploading ? 'Đang upload...' : 'Upload'}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {loading ? (
        <p className="text-muted-foreground">Đang tải...</p>
      ) : docs.length === 0 ? (
        <Card><CardContent className="py-8 text-center text-muted-foreground">
          Chưa có tài liệu nào.
        </CardContent></Card>
      ) : (
        <div className="rounded-lg border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-4 py-3 text-left font-medium">Loại file</th>
                <th className="px-4 py-3 text-left font-medium">Mô tả</th>
                <th className="px-4 py-3 text-left font-medium">Rev</th>
                <th className="px-4 py-3 text-left font-medium">Code / Seg</th>
                <th className="px-4 py-3 text-left font-medium">Máy</th>
                <th className="px-4 py-3 text-left font-medium">Trạng thái</th>
                <th className="px-4 py-3 text-left font-medium">Upload bởi</th>
                <th className="px-4 py-3 text-left font-medium"></th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {docs.map(doc => (
                <tr key={doc.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3">
                    <span className="rounded bg-muted px-2 py-0.5 text-xs font-mono">{doc.fileTypeCode}</span>
                    <span className="ml-2 text-muted-foreground">{doc.fileTypeName}</span>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{doc.description ?? '—'}</td>
                  <td className="px-4 py-3">{doc.revision ?? '—'}</td>
                  <td className="px-4 py-3 font-mono text-xs">{doc.code}{doc.segment ? ` [${doc.segment}]` : ''}</td>
                  <td className="px-4 py-3 text-muted-foreground">{doc.machineType ?? '—'}</td>
                  <td className="px-4 py-3">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_STYLE[doc.status] ?? 'bg-gray-100'}`}>
                      {doc.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{doc.createdByName}</td>
                  <td className="px-4 py-3">
                    {doc.status !== 'Rejected' && (
                      <Button size="sm" variant="outline" onClick={() => getDownloadUrl(doc.id)}>Xem</Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
