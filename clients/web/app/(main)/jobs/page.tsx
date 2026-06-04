'use client'

import { useState, useEffect, useCallback } from 'react'
import Link from 'next/link'
import { api, type JobDto } from '@/lib/api-client'
import { VATopbar, VABadge, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { CreateJobDialog } from '@/components/jobs/create-job-dialog'

export default function JobsPage() {
  const [jobs, setJobs] = useState<JobDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)

  const loadJobs = useCallback(async () => {
    setLoading(true)
    const res = await api.jobs.list(page, search || undefined)
    if (res.success && res.data) {
      setJobs(res.data)
      setTotal(res.pagination?.total ?? 0)
    }
    setLoading(false)
  }, [page, search])

  useEffect(() => { loadJobs() }, [loadJobs])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="Job & Part"
        breadcrumb="Sản xuất › Quản lý đơn hàng"
        right={<VABtn kind="primary" onClick={() => setShowCreate(true)}>+ Tạo Job</VABtn>}
      />
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22 }}>
      <div className="mb-6 flex items-center justify-between" style={{ display: 'none' }}>
        <h1 className="text-2xl font-semibold">Jobs</h1>
        <Button onClick={() => setShowCreate(true)}>+ Tạo Job</Button>
      </div>

      <div className="mb-4 flex gap-2">
        <Input
          placeholder="Tìm job number hoặc part number..."
          value={search}
          onChange={e => { setSearch(e.target.value); setPage(1) }}
          className="max-w-sm"
        />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Đang tải...</p>
      ) : jobs.length === 0 ? (
        <p className="text-muted-foreground">Không có job nào.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-4 py-3 text-left font-medium">Job Number</th>
                <th className="px-4 py-3 text-left font-medium">Part Number</th>
                <th className="px-4 py-3 text-left font-medium">Rev (Drawing)</th>
                <th className="px-4 py-3 text-left font-medium">Routing Rev</th>
                <th className="px-4 py-3 text-right font-medium">Run Qty</th>
                <th className="px-4 py-3 text-left font-medium">Ship By</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {jobs.map(job => (
                <tr key={job.id} className="hover:bg-muted/30 transition-colors">
                  <td className="px-4 py-3">
                    <Link href={`/jobs/${job.id}`} className="font-medium text-primary hover:underline">
                      {job.jobNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3 font-mono text-xs">{job.partNumber}</td>
                  <td className="px-4 py-3">{job.revCode}</td>
                  <td className="px-4 py-3 text-muted-foreground">{job.routingRevCode}</td>
                  <td className="px-4 py-3 text-right">{job.runQty ?? '—'}</td>
                  <td className="px-4 py-3">{job.shipBy ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
        <span>Tổng: {total} jobs</span>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Trước</Button>
          <span className="px-2 py-1">Trang {page}</span>
          <Button variant="outline" size="sm" disabled={jobs.length < 20} onClick={() => setPage(p => p + 1)}>Sau →</Button>
        </div>
      </div>

      <CreateJobDialog open={showCreate} onClose={() => setShowCreate(false)} onCreated={loadJobs} />
      </div>{/* end scroll */}
    </div>
  )
}
