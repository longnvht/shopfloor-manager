'use client'

import { useState, useEffect, useRef } from 'react'
import { useParams, useSearchParams, useRouter } from 'next/navigation'
import { api, type FaiSheetDto } from '@/lib/api-client'
import { VATopbar, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { FaiMatrix } from '@/components/fai/fai-matrix'

export default function FaiPage() {
  const { id }       = useParams<{ id: string }>()
  const searchParams = useSearchParams()
  const router       = useRouter()
  const opId         = searchParams.get('opId')

  const [sheet,   setSheet]   = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving,  setSaving]  = useState<string | null>(null)

  // Map to hold pending input values per cell (controlled)
  const inputValues = useRef<Record<string, string>>({})

  useEffect(() => {
    if (!opId) { setLoading(false); return }
    api.fai.sheet(Number(opId), Number(id)).then(res => {
      if (res.success) setSheet(res.data)
      setLoading(false)
    })
  }, [id, opId])

  async function handleMeasure(dimId: number, productId: number, rawValue: string) {
    const num = parseFloat(rawValue)
    if (isNaN(num)) return
    const key = `${dimId}-${productId}`
    setSaving(key)
    await api.fai.saveMeasure({ dimensionId: dimId, productId, value: num })
    const res = await api.fai.sheet(Number(opId), Number(id))
    if (res.success) {
      setSheet(res.data)
      // Clear stored input value after successful save
      delete inputValues.current[key]
    }
    setSaving(null)
  }

  // ── No opId state ──────────────────────────────────────────────────────────
  if (!opId) {
    return (
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
        <VATopbar title="FAI Sheet" breadcrumb={`Chất lượng › Job #${id} › FAI`}
          right={<VABtn kind="ghost" onClick={() => router.push(`/jobs/${id}`)}>← Quay lại Job</VABtn>} />
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
          <div style={{ fontSize: 28, color: va.text3 }}>◉</div>
          <div style={{ fontSize: 14, color: va.text2, fontWeight: 600 }}>Chưa chọn Operation</div>
          <div style={{ fontSize: 12.5, color: va.text3, textAlign: 'center', maxWidth: 320 }}>
            Vào Job Detail, chọn Operation cần xem FAI rồi bấm nút <strong>FAI Sheet</strong>.
          </div>
          <VABtn kind="primary" onClick={() => router.push(`/jobs/${id}`)}>← Về Job Detail</VABtn>
        </div>
      </div>
    )
  }

  // ── Loading / Error ────────────────────────────────────────────────────────
  if (loading) {
    return (
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
        <VATopbar title="FAI Sheet" breadcrumb={`Chất lượng › Job #${id} › OP ${opId}`} />
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>Đang tải FAI sheet…</div>
      </div>
    )
  }

  if (!sheet) {
    return (
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
        <VATopbar title="FAI Sheet" breadcrumb={`Chất lượng › Job #${id} › OP ${opId}`}
          right={<VABtn kind="ghost" onClick={() => router.push(`/jobs/${id}`)}>← Quay lại</VABtn>} />
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.err, fontSize: 13 }}>Không tải được FAI sheet.</div>
      </div>
    )
  }

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar
        title={`FAI Sheet — OP ${opId}`}
        breadcrumb={`Chất lượng › Job #${id} › FAI`}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            <VABtn kind="ghost" onClick={() => router.push(`/jobs/${id}`)}>← Job</VABtn>
          </div>
        }
      />
      <FaiMatrix sheet={sheet} onMeasure={handleMeasure} saving={saving} />
    </div>
  )
}
