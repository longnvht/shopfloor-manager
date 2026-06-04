'use client'

import { useState, useEffect, useCallback } from 'react'
import Link from 'next/link'
import { api, type PartDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { CreatePartDialog } from '@/components/parts/create-part-dialog'
import { VATopbar, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'

export default function PartsPage() {
  const [parts, setParts] = useState<PartDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    const res = await api.parts.list(page, search || undefined)
    if (res.success && res.data) {
      setParts(res.data)
      setTotal(res.pagination?.total ?? 0)
    }
    setLoading(false)
  }, [page, search])

  useEffect(() => { load() }, [load])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Parts & Routing" breadcrumb="Sản xuất › Quản lý chi tiết"
        right={<VABtn kind="primary" onClick={() => setShowCreate(true)}>+ Tạo Part</VABtn>} />
      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22 }}>
      <div className="mb-6 flex items-center justify-between" style={{ display: 'none' }}>
        <h1 className="text-2xl font-semibold">Parts</h1>
        <Button onClick={() => setShowCreate(true)}>+ Thêm Part</Button>
      </div>

      <div className="mb-4">
        <Input placeholder="Tìm PartNumber hoặc mô tả..."
          value={search}
          onChange={e => { setSearch(e.target.value); setPage(1) }}
          className="max-w-sm" />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Đang tải...</p>
      ) : parts.length === 0 ? (
        <p className="text-muted-foreground">Chưa có Part nào.</p>
      ) : (
        <div className="rounded-lg border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-4 py-3 text-left font-medium">Part Number</th>
                <th className="px-4 py-3 text-left font-medium">Mô tả</th>
                <th className="px-4 py-3 text-left font-medium">Ngày tạo</th>
                <th className="px-4 py-3 text-left font-medium"></th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {parts.map(part => (
                <tr key={part.id} className="hover:bg-muted/30 transition-colors">
                  <td className="px-4 py-3 font-mono font-medium">{part.partNumber}</td>
                  <td className="px-4 py-3 text-muted-foreground">{part.description}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(part.createdAt).toLocaleDateString('vi-VN')}
                  </td>
                  <td className="px-4 py-3">
                    <Link href={`/parts/${part.id}`}
                      className="text-sm text-primary hover:underline">
                      Xem chi tiết →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
        <span>Tổng: {total} parts</span>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Trước</Button>
          <span className="px-2 py-1">Trang {page}</span>
          <Button variant="outline" size="sm" disabled={parts.length < 20} onClick={() => setPage(p => p + 1)}>Sau →</Button>
        </div>
      </div>

      <CreatePartDialog open={showCreate} onClose={() => setShowCreate(false)} onCreated={load} />
      </div>{/* end scroll */}
    </div>
  )
}
