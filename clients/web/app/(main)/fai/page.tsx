'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { api, type PartOpDto, type FaiSheetDto, type JobDto, MEASURE_STAGE_LABELS } from '@/lib/api-client'
import { VATopbar, VABtn, VASeg } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { downloadBlob } from '@/lib/doc-format'
import { FaiMatrix } from '@/components/fai/fai-matrix'
import { FaiJobList } from '@/components/fai/fai-job-list'
import { FaiOpSelect } from '@/components/fai/fai-op-select'

const STAGE_OPTIONS = [
  { id: '0', label: MEASURE_STAGE_LABELS[0] },
  { id: '1', label: MEASURE_STAGE_LABELS[1] },
  { id: '2', label: MEASURE_STAGE_LABELS[2] },
]

export default function FaiPage() {
  const router = useRouter()

  const [jobId, setJobId] = useState<number | null>(null)
  const [ops, setOps] = useState<PartOpDto[]>([])
  const [opId, setOpId] = useState<number | null>(null)
  const [stageFilter, setStageFilter] = useState('0')

  const [sheet, setSheet] = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [exporting, setExporting] = useState<'excel' | 'pdf' | null>(null)

  const onSelectJob = useCallback((id: number, _job: JobDto) => {
    setJobId(id)
    setOpId(null)
    setSheet(null)
  }, [])

  useEffect(() => {
    if (!jobId) { setOps([]); return }
    api.jobs.operations(jobId).then(res => {
      if (res.success && res.data) {
        setOps(res.data)
        setOpId(0) // mặc định "Tất cả OP"
      }
    })
  }, [jobId])

  const loadSheet = useCallback(() => {
    if (!jobId || opId == null) { setSheet(null); return }
    setLoading(true)
    api.fai.sheet(opId === 0 ? null : opId, jobId).then(res => {
      if (res.success) setSheet(res.data)
      setLoading(false)
    })
  }, [jobId, opId])

  useEffect(() => { loadSheet() }, [loadSheet])

  async function handleExport(kind: 'excel' | 'pdf') {
    if (!sheet) return
    setExporting(kind)
    try {
      const stage = Number(stageFilter)
      const blob = kind === 'excel'
        ? await api.fai.exportExcel(sheet.partOpId === 0 ? null : sheet.partOpId, sheet.jobId, stage)
        : await api.fai.exportPdf(sheet.partOpId === 0 ? null : sheet.partOpId, sheet.jobId, stage)
      downloadBlob(blob, `FAI_OP${sheet.opNumber}.${kind === 'excel' ? 'xlsx' : 'pdf'}`)
    } finally {
      setExporting(null)
    }
  }

  return (
    <div style={{ flex: 1, display: 'flex', minWidth: 0, minHeight: 0, background: va.bg }}>
      <FaiJobList selectedJobId={jobId} onSelect={onSelectJob} />

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0 }}>
        <VATopbar title="FAI · Ma trận đo kiểm" breadcrumb="Chất lượng › FAI & Đo kiểm"
          right={<div style={{ display: 'flex', gap: 8 }}>
            <VABtn kind="ghost" onClick={() => handleExport('excel')} disabled={!sheet || exporting !== null}>
              {exporting === 'excel' ? 'Đang xuất…' : '⤓ Excel'}
            </VABtn>
            <VABtn kind="primary" onClick={() => handleExport('pdf')} disabled={!sheet || exporting !== null}>
              {exporting === 'pdf' ? 'Đang xuất…' : '⤓ Xuất FAI PDF'}
            </VABtn>
            {jobId && <VABtn kind="ghost" onClick={() => router.push(`/jobs/${jobId}`)}>→ Job</VABtn>}
          </div>} />

        {!jobId ? (
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
            <div style={{ fontSize: 28, color: va.text3 }}>◉</div>
            <div style={{ fontSize: 14, color: va.text2, fontWeight: 600 }}>Chọn một Job</div>
            <div style={{ fontSize: 12.5, color: va.text3, textAlign: 'center', maxWidth: 320 }}>
              Chọn Job ở panel bên trái để xem bảng đo FAI.
            </div>
          </div>
        ) : (
          <>
            <div style={{ margin: '22px 22px 0', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '12px 18px', boxShadow: va.shadow, display: 'flex', alignItems: 'center', gap: 18, flexWrap: 'wrap' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
                <span style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 700 }}>Operation</span>
                <FaiOpSelect ops={ops} value={opId} onChange={setOpId} />
              </div>
              <div style={{ height: 28, width: 1, background: va.separator }} />
              <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
                <span style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 700 }}>Measure Stage</span>
                <VASeg options={STAGE_OPTIONS} value={stageFilter} onChange={setStageFilter} />
              </div>
              {sheet && (
                <div style={{ marginLeft: 'auto', fontSize: 11, color: va.text3 }}>
                  {sheet.dimensions.length} balloon hiển thị
                </div>
              )}
            </div>

            {opId == null ? (
              <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 12 }}>
                <div style={{ fontSize: 14, color: va.text2, fontWeight: 600 }}>Chọn Operation</div>
                <div style={{ fontSize: 12.5, color: va.text3, textAlign: 'center', maxWidth: 320 }}>
                  Job này chưa có Operation nào — hoặc chưa chọn Operation cần xem.
                </div>
              </div>
            ) : loading ? (
              <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>Đang tải FAI sheet…</div>
            ) : !sheet ? (
              <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.err, fontSize: 13 }}>Không tải được FAI sheet.</div>
            ) : (
              <FaiMatrix sheet={sheet} stageFilter={stageFilter} />
            )}
          </>
        )}
      </div>
    </div>
  )
}
