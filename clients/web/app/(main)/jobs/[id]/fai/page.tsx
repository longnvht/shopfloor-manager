'use client'

import { useState, useEffect } from 'react'
import { useParams, useSearchParams, useRouter } from 'next/navigation'
import { api, type FaiSheetDto, MEASURE_STAGE_LABELS } from '@/lib/api-client'
import { VATopbar, VABtn, VASeg } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { downloadBlob } from '@/lib/doc-format'
import { FaiMatrix } from '@/components/fai/fai-matrix'

const STAGE_OPTIONS = [
  { id: '0', label: MEASURE_STAGE_LABELS[0] },
  { id: '1', label: MEASURE_STAGE_LABELS[1] },
  { id: '2', label: MEASURE_STAGE_LABELS[2] },
]

export default function FaiPage() {
  const { id }       = useParams<{ id: string }>()
  const searchParams = useSearchParams()
  const router       = useRouter()
  const opId         = searchParams.get('opId')

  const [sheet,   setSheet]   = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [stageFilter, setStageFilter] = useState('0')
  const [exporting, setExporting] = useState<'excel' | 'pdf' | null>(null)

  useEffect(() => {
    if (!opId) { setLoading(false); return }
    api.fai.sheet(Number(opId), Number(id)).then(res => {
      if (res.success) setSheet(res.data)
      setLoading(false)
    })
  }, [id, opId])

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

  async function handleExport(kind: 'excel' | 'pdf') {
    if (!sheet) return
    setExporting(kind)
    try {
      const stage = Number(stageFilter)
      const blob = kind === 'excel'
        ? await api.fai.exportExcel(sheet.partOpId, sheet.jobId, stage)
        : await api.fai.exportPdf(sheet.partOpId, sheet.jobId, stage)
      downloadBlob(blob, `FAI_OP${sheet.opNumber}.${kind === 'excel' ? 'xlsx' : 'pdf'}`)
    } finally {
      setExporting(null)
    }
  }

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar
        title={`FAI Sheet — OP ${opId}`}
        breadcrumb={`Chất lượng › Job #${id} › FAI`}
        right={
          <div style={{ display: 'flex', gap: 8 }}>
            <VABtn kind="ghost" onClick={() => handleExport('excel')} disabled={exporting !== null}>
              {exporting === 'excel' ? 'Đang xuất…' : '⤓ Excel'}
            </VABtn>
            <VABtn kind="primary" onClick={() => handleExport('pdf')} disabled={exporting !== null}>
              {exporting === 'pdf' ? 'Đang xuất…' : '⤓ Xuất FAI PDF'}
            </VABtn>
            <VABtn kind="ghost" onClick={() => router.push(`/jobs/${id}`)}>← Job</VABtn>
          </div>
        }
      />
      <div style={{ padding: '10px 22px', background: va.surface, borderBottom: `1px solid ${va.border}`, display: 'flex', alignItems: 'center', gap: 18, flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
          <span style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 700 }}>Measure Stage</span>
          <VASeg options={STAGE_OPTIONS} value={stageFilter} onChange={setStageFilter} />
        </div>
        <div style={{ marginLeft: 'auto', fontSize: 11, color: va.text3 }}>
          {sheet.dimensions.length} balloon hiển thị
        </div>
      </div>
      <FaiMatrix sheet={sheet} stageFilter={stageFilter} />
    </div>
  )
}
