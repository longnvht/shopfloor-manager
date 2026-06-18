'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { api, type PartOpDto, type FaiSheetDto, type JobDto } from '@/lib/api-client'
import { VATopbar, VABtn } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { FaiMatrix } from '@/components/fai/fai-matrix'
import { FaiJobList } from '@/components/fai/fai-job-list'
import { FaiOpSelect } from '@/components/fai/fai-op-select'

export default function FaiPage() {
  const router = useRouter()

  const [jobId, setJobId] = useState<number | null>(null)
  const [ops, setOps] = useState<PartOpDto[]>([])
  const [opId, setOpId] = useState<number | null>(null)

  const [sheet, setSheet] = useState<FaiSheetDto | null>(null)
  const [loading, setLoading] = useState(false)

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
        const firstWithSheet = res.data.find(o => o.dimCount > 0)
        setOpId(firstWithSheet ? firstWithSheet.id : null)
      }
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

  return (
    <div style={{ flex: 1, display: 'flex', minWidth: 0, minHeight: 0, background: va.bg }}>
      <FaiJobList selectedJobId={jobId} onSelect={onSelectJob} />

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0 }}>
        <VATopbar title="FAI · Ma trận đo kiểm" breadcrumb="Chất lượng › FAI & Đo kiểm"
          right={jobId ? <VABtn kind="ghost" onClick={() => router.push(`/jobs/${jobId}`)}>→ Job</VABtn> : undefined} />

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
            <div style={{ padding: '10px 22px', background: va.surface, borderBottom: `1px solid ${va.border}` }}>
              <FaiOpSelect ops={ops} value={opId} onChange={setOpId} />
            </div>

            {!opId ? (
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
              <FaiMatrix sheet={sheet} />
            )}
          </>
        )}
      </div>
    </div>
  )
}
