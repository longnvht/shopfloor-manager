'use client'

import { useState, useEffect } from 'react'
import { useParams } from 'next/navigation'
import Link from 'next/link'
import { api, type PartRevDto, type RoutingRevDto, type PartOpDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export default function PartDetailPage() {
  const { id } = useParams<{ id: string }>()
  const partId = Number(id)

  const [revs, setRevs]               = useState<PartRevDto[]>([])
  const [selectedRev, setSelectedRev] = useState<PartRevDto | null>(null)
  const [routingRevs, setRoutingRevs] = useState<RoutingRevDto[]>([])
  const [selectedRR, setSelectedRR]   = useState<RoutingRevDto | null>(null)
  const [ops, setOps]                 = useState<PartOpDto[]>([])
  const [loading, setLoading]         = useState(true)

  useEffect(() => {
    api.parts.revisions(partId).then(res => {
      if (res.success && res.data) {
        setRevs(res.data)
        const active = res.data.find(r => r.isActive) ?? res.data[0]
        if (active) setSelectedRev(active)
      }
      setLoading(false)
    })
  }, [partId])

  useEffect(() => {
    if (!selectedRev) return
    api.parts.routingRevs(selectedRev.id, 1).then(res => {
      if (res.success && res.data) {
        setRoutingRevs(res.data)
        const active = res.data.find(r => r.isActive) ?? res.data[0]
        if (active) setSelectedRR(active)
      }
    })
  }, [selectedRev])

  useEffect(() => {
    if (!selectedRR) { setOps([]); return }
    api.operations.listForRoutingRev(selectedRR.id).then(res => {
      if (res.success && res.data) setOps(res.data)
    })
  }, [selectedRR])

  if (loading) return <p className="text-muted-foreground">Đang tải...</p>

  const partNumber = revs[0]?.partNumber ?? `Part #${partId}`

  // Build URL params for documents page
  const docBaseParams = (extra: Record<string, string>) =>
    new URLSearchParams({ partNumber, ...extra }).toString()

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Link href="/parts" className="text-muted-foreground hover:text-foreground">← Parts</Link>
        <span className="text-muted-foreground">/</span>
        <h1 className="text-2xl font-semibold font-mono">{partNumber}</h1>
      </div>

      {/* PartRev selector */}
      <Card>
        <CardHeader><CardTitle>Drawing Revisions</CardTitle></CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {revs.map(rev => (
              <button key={rev.id}
                onClick={() => setSelectedRev(rev)}
                className={`rounded-md border px-3 py-1.5 text-sm transition-colors ${
                  selectedRev?.id === rev.id
                    ? 'border-primary bg-primary text-primary-foreground'
                    : 'hover:bg-muted'
                }`}>
                Rev {rev.revCode}
                {rev.isActive && <span className="ml-1 text-xs opacity-70">(active)</span>}
              </button>
            ))}
          </div>
          {selectedRev?.description && (
            <p className="mt-2 text-sm text-muted-foreground">{selectedRev.description}</p>
          )}

          {/* Part-level documents (DRW, CAD) */}
          {selectedRev && (
            <div className="mt-3 flex gap-2">
              <Link href={`/parts/${id}/documents?partRevId=${selectedRev.id}&${docBaseParams({ revCode: selectedRev.revCode })}`}>
                <Button size="sm" variant="outline">
                  Bản vẽ / CAD (Rev {selectedRev.revCode})
                </Button>
              </Link>
            </div>
          )}
        </CardContent>
      </Card>

      {/* RoutingRev selector */}
      {routingRevs.length > 0 && (
        <Card>
          <CardHeader><CardTitle>Routing Revisions</CardTitle></CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {routingRevs.map(rr => (
                <button key={rr.id}
                  onClick={() => setSelectedRR(rr)}
                  className={`rounded-md border px-3 py-1.5 text-sm transition-colors ${
                    selectedRR?.id === rr.id
                      ? 'border-primary bg-primary text-primary-foreground'
                      : 'hover:bg-muted'
                  }`}>
                  {rr.revCode}
                  <span className="ml-1 text-xs opacity-70">({rr.opCount} OPs)</span>
                  {rr.isActive && <span className="ml-1 text-xs opacity-70">(active)</span>}
                </button>
              ))}
            </div>
            {selectedRR?.changeNote && (
              <p className="mt-2 text-sm text-muted-foreground">{selectedRR.changeNote}</p>
            )}
          </CardContent>
        </Card>
      )}

      {/* Operations list */}
      <Card>
        <CardHeader>
          <CardTitle>
            Operations
            {selectedRR && <span className="ml-2 text-sm font-normal text-muted-foreground">(Routing {selectedRR.revCode})</span>}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {ops.length === 0 ? (
            <p className="text-sm text-muted-foreground">Chưa có operation nào.</p>
          ) : (
            <table className="w-full text-sm">
              <thead><tr className="border-b">
                <th className="pb-2 text-left font-medium">OP</th>
                <th className="pb-2 text-left font-medium">Loại</th>
                <th className="pb-2 text-left font-medium">Mô tả</th>
                <th className="pb-2 text-right font-medium">Setup (h)</th>
                <th className="pb-2 text-right font-medium">Prod (h)</th>
                <th className="pb-2 text-center font-medium">Trạng thái</th>
                <th className="pb-2 text-center font-medium">Tài liệu</th>
              </tr></thead>
              <tbody className="divide-y">
                {ops.map(op => (
                  <tr key={op.id} className="hover:bg-muted/20">
                    <td className="py-2 font-mono font-medium">{op.opNumber}</td>
                    <td className="py-2 text-muted-foreground">{op.opTypeName ?? '—'}</td>
                    <td className="py-2 text-muted-foreground max-w-xs truncate">{op.description ?? '—'}</td>
                    <td className="py-2 text-right">{op.setupTime ?? '—'}</td>
                    <td className="py-2 text-right">{op.prodTime ?? '—'}</td>
                    <td className="py-2 text-center">
                      <span className={`rounded-full px-2 py-0.5 text-xs ${
                        op.isComplete ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'
                      }`}>
                        {op.isComplete ? 'Done' : 'Active'}
                      </span>
                    </td>
                    <td className="py-2 text-center">
                      <Link href={`/parts/${id}/documents?opId=${op.id}&opNumber=${op.opNumber}&${docBaseParams({ revCode: selectedRev?.revCode ?? '' })}`}>
                        <Button size="sm" variant="ghost" className="h-7 text-xs">
                          Tài liệu →
                        </Button>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
