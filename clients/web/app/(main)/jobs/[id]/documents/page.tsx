'use client'

import { useState, useEffect, useCallback } from 'react'
import { useParams, useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

type TechDocDto = {
  id: number; fileTypeCode: string; fileTypeName: string
  partRevId: number | null; partOpId: number | null; jobId: number | null
  description: string | null; revision: string | null
  code: string | null; segment: string | null; machineType: string | null
  status: string; createdByName: string; createdAt: string
}

const STATUS_STYLE: Record<string, string> = {
  Pending:  'bg-yellow-100 text-yellow-700',
  Approved: 'bg-green-100 text-green-700',
  Rejected: 'bg-red-100 text-red-700',
}

const authHeader = () => ({ Authorization: `Bearer ${localStorage.getItem('auth-token')}` })
const apiBase = process.env.NEXT_PUBLIC_API_URL

export default function JobDocumentsPage() {
  const { id } = useParams<{ id: string }>()
  const searchParams = useSearchParams()
  const opId     = searchParams.get('opId')
  const opNumber = searchParams.get('opNumber')

  const [docs, setDocs]     = useState<TechDocDto[]>([])
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    const params = new URLSearchParams()
    if (opId) params.set('partOpId', opId)
    else params.set('jobId', id)

    const res = await fetch(`${apiBase}/api/v1/tech-documents?${params}`, { headers: authHeader() })
    const data = await res.json()
    if (data.success) setDocs(data.data ?? [])
    setLoading(false)
  }, [id, opId])

  useEffect(() => { load() }, [load])

  async function getDownloadUrl(docId: number) {
    const res = await fetch(`${apiBase}/api/v1/tech-documents/${docId}/download-url`, { headers: authHeader() })
    const data = await res.json()
    if (data.success && data.data) window.open(data.data, '_blank')
  }

  const title = opId ? `Custom OP ${opNumber ?? opId} — Tài liệu` : 'Tài liệu Job'

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Link href={`/jobs/${id}`} className="text-muted-foreground hover:text-foreground">← Job Detail</Link>
        <span className="text-muted-foreground">/</span>
        <h1 className="text-xl font-semibold">{title}</h1>
        <span className="rounded bg-yellow-100 text-yellow-700 px-2 py-0.5 text-xs font-medium">Custom OP</span>
      </div>

      {loading ? (
        <p className="text-muted-foreground">Đang tải...</p>
      ) : docs.length === 0 ? (
        <Card><CardContent className="py-8 text-center text-muted-foreground">
          Chưa có tài liệu nào cho OP này.
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
