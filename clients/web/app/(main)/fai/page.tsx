'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { api, type JobDto, type PartOpDto, type FaiSheetDto } from '@/lib/api-client'
import { VATopbar, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { FaiMatrix } from '@/components/fai/fai-matrix'

const selectClass = { padding: '6px 12px', borderRadius: 7, border: `1px solid ${va.border}`, fontSize: 12.5, fontFamily: va.font, background: va.surface, color: va.text, outline: 'none' }

export default function FaiPage() {
  const router = useRouter()

  const [search,  setSearch]  = useState('')
  const [jobs,    setJobs]    = useState<JobDto[]>([])
  const [jobId,   setJobId]   = useState<number | null>(null)

  const [ops,   setOps]   = useState<PartOpDto[]>([])
  const [opId,  setOpId]  = useState<number | null>(null)

  const [sheet,   setSheet]   = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [saving,  setSaving]  = useState<string | null>(null)

  // Load job list (search)
  useEffect(() => {
    api.jobs.list(1, search || undefined).then(res => {
      if (res.success && res.data) setJobs(res.data)
    })
  }, [search])

  // Load operations when job changes
  useEffect(() => {
    setOpId(null)
    setSheet(null)
    if (!jobId) { setOps([]); return }
    api.jobs.operations(jobId).then(res => {
      if (res.success && res.data) setOps(res.data)
    })
  }, [jobId])

  const loadSheet = useCallback(() => {
    if (!jobId || !opId) { setSheet(null); return }
    setLoading(true)
    api.fai.sheet(opId, jobId).then(res => {
      if (res.success) setSheet(res.data)
      setLoading(false)
    })
  }, [jobId, opId])

  useEffect(() => { loadSheet() }, [loadSheet])

  async function handleMeasure(dimId: number, productId: number, rawValue: string) {
    const num = parseFloat(rawValue)
    if (isNaN(num)) return
    const key = `${dimId}-${productId}`
    setSaving(key)
    await api.fai.saveMeasure({ dimensionId: dimId, productId, value: num })
    if (jobId && opId) {
      const res = await api.fai.sheet(opId, jobId)
      if (res.success) setSheet(res.data)
    }
    setSaving(null)
  }

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar title="FAI · Dimension Sheet" breadcrumb="Chất lượng › FAI & Đo kiểm"
        right={jobId ? <VABtn kind="ghost" onClick={() => router.push(`/jobs/${jobId}`)}>→ Job</VABtn> : undefined} />

      {/* Job / OP selector */}
      <div style={{ padding: '10px 22px', background: va.surface, borderBottom: `1px solid ${va.border}`, display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
        <div style={{ height: 34, minWidth: 200, background: va.bg, border: `1px solid ${va.border}`, borderRadius: 7, padding: '0 12px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 12.5, color: va.text3 }}>
          <span>⌕</span>
          <input value={search} onChange={e => setSearch(e.target.value)}
            placeholder="Tìm Job number…"
            style={{ border: 'none', background: 'transparent', outline: 'none', flex: 1, fontSize: 12.5, color: va.text, fontFamily: va.font }} />
        </div>

        <select value={jobId ?? ''} onChange={e => setJobId(e.target.value ? Number(e.target.value) : null)} style={{ ...selectClass, minWidth: 220 }}>
          <option value="">— Chọn Job —</option>
          {jobs.map(j => <option key={j.id} value={j.id}>{j.jobNumber} · {j.partNumber} Rev {j.revCode}</option>)}
        </select>

        <select value={opId ?? ''} onChange={e => setOpId(e.target.value ? Number(e.target.value) : null)} disabled={!jobId} style={{ ...selectClass, minWidth: 220, opacity: jobId ? 1 : 0.5 }}>
          <option value="">— Chọn Operation —</option>
          {ops.map(o => <option key={o.id} value={o.id}>OP{o.opNumber} {o.description ? `· ${o.description}` : ''}</option>)}
        </select>
      </div>

      {/* Content */}
      {!jobId || !opId ? (
        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
          <div style={{ fontSize: 28, color: va.text3 }}>◉</div>
          <div style={{ fontSize: 14, color: va.text2, fontWeight: 600 }}>Chọn Job và Operation</div>
          <div style={{ fontSize: 12.5, color: va.text3, textAlign: 'center', maxWidth: 320 }}>
            Chọn Job rồi chọn Operation cần xem bảng đo FAI.
          </div>
        </div>
      ) : loading ? (
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>Đang tải FAI sheet…</div>
      ) : !sheet ? (
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.err, fontSize: 13 }}>Không tải được FAI sheet.</div>
      ) : (
        <FaiMatrix sheet={sheet} onMeasure={handleMeasure} saving={saving} />
      )}
    </div>
  )
}
