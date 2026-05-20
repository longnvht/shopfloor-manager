'use client'

import { useState, useEffect } from 'react'
import { useParams, useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { api, type FaiSheetDto } from '@/lib/api-client'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export default function FaiPage() {
  const { id } = useParams<{ id: string }>()
  const searchParams = useSearchParams()
  const opId = searchParams.get('opId')

  const [sheet, setSheet] = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState<string | null>(null)

  useEffect(() => {
    if (!opId) { setLoading(false); return }
    api.fai.sheet(Number(opId), Number(id)).then(res => {
      if (res.success) setSheet(res.data)
      setLoading(false)
    })
  }, [id, opId])

  async function handleMeasure(dimId: number, productId: number, value: string) {
    const num = parseFloat(value)
    if (isNaN(num)) return
    setSaving(`${dimId}-${productId}`)
    await api.fai.saveMeasure({ dimensionId: dimId, productId, value: num })
    // Reload sheet
    const res = await api.fai.sheet(Number(opId), Number(id))
    if (res.success) setSheet(res.data)
    setSaving(null)
  }

  if (!opId) return (
    <div>
      <Link href={`/jobs/${id}`} className="text-muted-foreground hover:text-foreground">← Job Detail</Link>
      <p className="mt-4 text-destructive">Thiếu opId. Vào Job Detail rồi chọn operation.</p>
    </div>
  )

  if (loading) return <p className="text-muted-foreground">Đang tải FAI sheet...</p>
  if (!sheet) return <p className="text-destructive">Không thể tải FAI sheet.</p>

  const dims = sheet.dimensions
  const rows = sheet.rows

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Link href={`/jobs/${id}`} className="text-muted-foreground hover:text-foreground">← Job Detail</Link>
        <span className="text-muted-foreground">/</span>
        <h1 className="text-xl font-semibold">FAI Sheet — OP {opId}</h1>
      </div>

      {dims.length === 0 ? (
        <Card><CardContent className="py-8 text-center text-muted-foreground">
          Operation này chưa có dimension. Cần thêm dimensions trước.
        </CardContent></Card>
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="sticky left-0 bg-muted/50 px-3 py-3 text-left font-medium min-w-[80px]">Serial</th>
                {dims.map(d => (
                  <th key={d.id} className="px-3 py-2 text-center font-medium min-w-[110px]">
                    <div className={`font-semibold ${d.isCritical ? 'text-red-600' : ''}`}>{d.code}</div>
                    <div className="text-xs font-normal text-muted-foreground">
                      {d.nominal} {d.upperTol >= 0 ? '+' : ''}{d.upperTol} / {d.lowerTol}
                    </div>
                    <div className="text-xs font-normal text-muted-foreground">{d.unit}</div>
                  </th>
                ))}
                <th className="px-3 py-3 text-center font-medium">Kết quả</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {rows.map(row => (
                <tr key={row.productId} className="hover:bg-muted/20">
                  <td className="sticky left-0 bg-background px-3 py-2 font-mono font-medium">{row.serialNumber}</td>
                  {row.cells.map((cell, i) => {
                    const dim = dims[i]
                    const key = `${dim.id}-${row.productId}`
                    const isSaving = saving === key
                    const bgColor = cell.result === 'Pass' ? 'bg-green-50' : cell.result === 'Fail' ? 'bg-red-50' : ''
                    return (
                      <td key={dim.id} className={`px-2 py-1 text-center ${bgColor}`}>
                        <Input
                          type="number"
                          step="0.001"
                          defaultValue={cell.value ?? ''}
                          className="w-24 text-center h-7 text-xs mx-auto"
                          disabled={isSaving}
                          onBlur={e => handleMeasure(dim.id, row.productId, e.target.value)}
                          onKeyDown={e => e.key === 'Enter' && handleMeasure(dim.id, row.productId, (e.target as HTMLInputElement).value)}
                        />
                      </td>
                    )
                  })}
                  <td className="px-3 py-2 text-center">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                      row.allPass ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                    }`}>
                      {row.allPass ? 'PASS' : 'FAIL'}
                    </span>
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
