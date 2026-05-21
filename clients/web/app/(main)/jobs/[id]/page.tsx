'use client'

import { useState, useEffect } from 'react'
import { useParams } from 'next/navigation'
import Link from 'next/link'
import { api, type JobDetailDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export default function JobDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [job, setJob] = useState<JobDetailDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.jobs.get(Number(id)).then(res => {
      if (res.success) setJob(res.data)
      setLoading(false)
    })
  }, [id])

  if (loading) return <p className="text-muted-foreground">Đang tải...</p>
  if (!job) return <p className="text-destructive">Không tìm thấy job.</p>

  const doneOps   = job.operations.filter(o => o.isComplete).length
  const doneProds = job.products.filter(p => p.isComplete).length

  // Tách standard OPs (thuộc routing) và ForJobOnly OPs (bất thường chỉ của job này)
  const standardOps   = job.operations.filter(o => !o.forJobOnly)
  const forJobOnlyOps = job.operations.filter(o => o.forJobOnly)

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Link href="/jobs" className="text-muted-foreground hover:text-foreground">← Jobs</Link>
        <span className="text-muted-foreground">/</span>
        <h1 className="text-2xl font-semibold">{job.jobNumber}</h1>
      </div>

      {/* Job info */}
      <Card>
        <CardHeader><CardTitle>Thông tin</CardTitle></CardHeader>
        <CardContent className="grid grid-cols-2 gap-4 text-sm md:grid-cols-4">
          <div><p className="text-muted-foreground">Part Number</p><p className="font-mono font-medium">{job.partNumber}</p></div>
          <div><p className="text-muted-foreground">Mô tả</p><p>{job.partDescription}</p></div>
          <div><p className="text-muted-foreground">Drawing Rev</p><p>{job.revCode}</p></div>
          <div><p className="text-muted-foreground">Routing Rev</p><p>{job.routingRevCode}</p></div>
          <div><p className="text-muted-foreground">Run Qty</p><p>{job.runQty ?? '—'}</p></div>
          <div><p className="text-muted-foreground">Ship By</p><p>{job.shipBy ?? '—'}</p></div>
          <div><p className="text-muted-foreground">OPs</p><p>{doneOps}/{job.operations.length} hoàn tất</p></div>
          <div><p className="text-muted-foreground">Serials</p><p>{doneProds}/{job.products.length} hoàn tất</p></div>
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Standard Operations — xem tài liệu từ routing (link sang Parts page) */}
        <Card>
          <CardHeader>
            <CardTitle>Standard Operations ({standardOps.length})</CardTitle>
          </CardHeader>
          <CardContent>
            {standardOps.length === 0 ? (
              <p className="text-sm text-muted-foreground">Chưa có operation.</p>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b">
                  <th className="pb-2 text-left font-medium">OP</th>
                  <th className="pb-2 text-left font-medium">Type</th>
                  <th className="pb-2 text-left font-medium">Mô tả</th>
                  <th className="pb-2 text-center font-medium">TT</th>
                  <th className="pb-2 text-center font-medium">Tài liệu</th>
                </tr></thead>
                <tbody className="divide-y">
                  {standardOps.map(op => (
                    <tr key={op.id}>
                      <td className="py-2 font-mono">{op.opNumber}</td>
                      <td className="py-2 text-muted-foreground">{op.opTypeName ?? '—'}</td>
                      <td className="py-2 text-muted-foreground truncate max-w-[120px]">{op.description ?? '—'}</td>
                      <td className="py-2 text-center">
                        <span className={`rounded-full px-2 py-0.5 text-xs ${op.isComplete ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
                          {op.isComplete ? 'Done' : '—'}
                        </span>
                      </td>
                      <td className="py-2 text-center">
                        {/* Standard OP docs sống ở Parts page */}
                        <Link href={`/parts/${job.partRevId}/documents?opId=${op.id}&opNumber=${op.opNumber}&partNumber=${job.partNumber}&revCode=${job.revCode}`}>
                          <Button size="sm" variant="ghost" className="h-7 text-xs">Xem →</Button>
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </CardContent>
        </Card>

        {/* ForJobOnly Operations — tài liệu job-specific quản lý tại đây */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Custom OPs — Job này ({forJobOnlyOps.length})</CardTitle>
            </div>
          </CardHeader>
          <CardContent>
            {forJobOnlyOps.length === 0 ? (
              <p className="text-sm text-muted-foreground">Không có OP bất thường.</p>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b">
                  <th className="pb-2 text-left font-medium">OP</th>
                  <th className="pb-2 text-left font-medium">Type</th>
                  <th className="pb-2 text-left font-medium">Mô tả</th>
                  <th className="pb-2 text-center font-medium">Tài liệu</th>
                </tr></thead>
                <tbody className="divide-y">
                  {forJobOnlyOps.map(op => (
                    <tr key={op.id}>
                      <td className="py-2 font-mono">{op.opNumber}</td>
                      <td className="py-2 text-muted-foreground">{op.opTypeName ?? '—'}</td>
                      <td className="py-2 text-muted-foreground truncate max-w-[140px]">{op.description ?? '—'}</td>
                      <td className="py-2 text-center">
                        {/* ForJobOnly OP docs quản lý tại jobs page */}
                        <Link href={`/jobs/${id}/documents?opId=${op.id}&opNumber=${op.opNumber}`}>
                          <Button size="sm" variant="outline" className="h-7 text-xs">Quản lý →</Button>
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

      {/* Products / Serials */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Product Serials ({job.products.length})</CardTitle>
            <div className="flex gap-2">
              <Link href={`/jobs/${id}/fai`}>
                <Button size="sm" variant="outline">FAI Sheet</Button>
              </Link>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {job.products.length === 0 ? (
            <p className="text-sm text-muted-foreground">Chưa có serial nào.</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {job.products.map(p => (
                <span key={p.id}
                  className={`rounded-md px-3 py-1 text-sm font-mono ${
                    p.isComplete ? 'bg-green-100 text-green-700' : 'bg-muted text-muted-foreground'
                  }`}>
                  {p.serialNumber}
                </span>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
