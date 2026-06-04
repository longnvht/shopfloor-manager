'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type NcrDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { VATopbar, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'

const STATUS_LABELS: Record<string, { label: string; className: string }> = {
  Open:   { label: 'Đang mở', className: 'bg-red-100 text-red-700' },
  Closed: { label: 'Đã đóng', className: 'bg-green-100 text-green-700' },
}

export default function NcrsPage() {
  const [ncrs, setNcrs] = useState<NcrDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [filter, setFilter] = useState('')
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.ncrs.list(page, filter || undefined)
    if (res.success && res.data) {
      setNcrs(res.data)
      setTotal(res.pagination?.total ?? 0)
    }
    setLoading(false)
  }, [page, filter])

  useEffect(() => { load() }, [load])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="NCR · Báo cáo không phù hợp" breadcrumb="Chất lượng › Non-Conformance"
        right={<VABtn kind="accent">+ Tạo NCR</VABtn>} />
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22 }}>
      <div className="mb-6 flex items-center justify-between" style={{ display: 'none' }}>
        <h1 className="text-2xl font-semibold">NCR — Non-Conformance Reports</h1>
        <div className="flex gap-2">
          {['', 'Open', 'Closed'].map(s => (
            <Button key={s} size="sm"
              variant={filter === s ? 'default' : 'outline'}
              onClick={() => { setFilter(s); setPage(1) }}>
              {s === '' ? 'Tất cả' : STATUS_LABELS[s].label}
            </Button>
          ))}
        </div>
      </div>

      {loading ? (
        <p className="text-muted-foreground">Đang tải...</p>
      ) : ncrs.length === 0 ? (
        <p className="text-muted-foreground">Không có NCR nào.</p>
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-4 py-3 text-left font-medium">NCR Number</th>
                <th className="px-4 py-3 text-left font-medium">Job</th>
                <th className="px-4 py-3 text-left font-medium">Mô tả</th>
                <th className="px-4 py-3 text-left font-medium">Trạng thái</th>
                <th className="px-4 py-3 text-left font-medium">Người tạo</th>
                <th className="px-4 py-3 text-left font-medium">Ngày tạo</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {ncrs.map(ncr => {
                const s = STATUS_LABELS[ncr.status] ?? { label: ncr.status, className: 'bg-gray-100 text-gray-600' }
                return (
                  <tr key={ncr.id} className="hover:bg-muted/30 transition-colors">
                    <td className="px-4 py-3 font-mono font-medium">{ncr.ncrNumber}</td>
                    <td className="px-4 py-3">{ncr.jobNumber}</td>
                    <td className="px-4 py-3 text-muted-foreground max-w-xs truncate">{ncr.description}</td>
                    <td className="px-4 py-3">
                      <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${s.className}`}>
                        {s.label}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{ncr.raisedBy}</td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Date(ncr.raisedAt).toLocaleDateString('vi-VN')}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
        <span>Tổng: {total} NCRs</span>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Trước</Button>
          <span className="px-2 py-1">Trang {page}</span>
          <Button variant="outline" size="sm" disabled={ncrs.length < 20} onClick={() => setPage(p => p + 1)}>Sau →</Button>
        </div>
      </div>
      </div>{/* end scroll */}
    </div>
  )
}
