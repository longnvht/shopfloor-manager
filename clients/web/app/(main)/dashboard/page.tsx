'use client'

import { useState, useEffect } from 'react'
import { api } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABadge } from '@/components/va'
import { VASeg } from '@/components/va/seg'
import { va } from '@/lib/va-tokens'

type Overview    = { activeJobs: number; completedJobsThisMonth: number; openNcrs: number; totalParts: number; totalGages: number; gagesExpiredOrDamaged: number; pendingCalibRequests: number; passRatePercent: number }
type Production  = { jobsRunning: number; serialsDoneToday: number; serialsTotal: number; avgProgressPercent: number }
type Quality     = { totalMeasured: number; passCount: number; failCount: number; passRate: number; openNcrs: number; closedNcrsThisMonth: number }
type MachineStatus = { machineId: number; machineCode: string; machineName: string | null; runMode: string | null; alarmMessage: string | null; spindleSpeed: number | null; partCount: number | null; lastSeen: string }

export default function DashboardPage() {
  const [overview,   setOverview]   = useState<Overview | null>(null)
  const [production, setProduction] = useState<Production | null>(null)
  const [quality,    setQuality]    = useState<Quality | null>(null)
  const [machines,   setMachines]   = useState<MachineStatus[]>([])
  const [period, setPeriod]         = useState('week')
  const [loading, setLoading]       = useState(true)

  useEffect(() => {
    Promise.all([
      api.dashboard.overview(),
      api.dashboard.production(),
      api.dashboard.quality(),
      api.machines.status(),
    ]).then(([ov, prod, qual, mach]) => {
      if (ov.success)   setOverview(ov.data as Overview)
      if (prod.success) setProduction(prod.data as Production)
      if (qual.success) setQuality(qual.data as Quality)
      if (mach.success) setMachines(mach.data as MachineStatus[])
      setLoading(false)
    })
  }, [])

  const runningMachines = machines.filter(m =>
    m.runMode === 'START' || m.runMode === 'ACTIVE').length

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar
        title="Dashboard Sản xuất"
        breadcrumb={`Xưởng · ${new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' })}`}
        right={
          <VASeg value={period} onChange={setPeriod}
            options={[{ id: 'day', label: 'Ngày' }, { id: 'week', label: 'Tuần' }, { id: 'month', label: 'Tháng' }, { id: 'quarter', label: 'Quý' }]} />
        }
      />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* KPI row */}
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Máy đang chạy"   value={loading ? '…' : `${runningMachines}/${machines.filter(m => m.machineName != null).length}`} sub="máy" />
          <VAKpi label="Job đang thực hiện" value={loading ? '…' : overview?.activeJobs ?? 0} />
          <VAKpi label="Tỉ lệ Pass FAI"  value={loading ? '…' : `${quality?.passRate ?? 0}%`} accent={va.ok} />
          <VAKpi label="NCR đang mở"      value={loading ? '…' : overview?.openNcrs ?? 0} sub="cần xử lý" accent={va.err} />
          <VAKpi label="SP hoàn thành hôm nay" value={loading ? '…' : production?.serialsDoneToday ?? 0} />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1.55fr 1fr', gap: 14, flex: 1, minHeight: 0 }}>
          {/* Machine status grid */}
          <VACard title="Trạng thái máy CNC" sub="Real-time qua MQTT" pad={false}
            right={
              <span style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: va.ok, fontWeight: 600 }}>
                <span style={{ width: 7, height: 7, borderRadius: '50%', background: va.ok, boxShadow: `0 0 0 3px ${va.ok}22` }} />
                Kết nối
              </span>
            }>
            <div className="va-scroll" style={{ padding: 13, display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 11, overflow: 'auto', height: '100%', alignContent: 'start' }}>
              {machines.length === 0 && !loading && (
                <div style={{ gridColumn: '1 / -1', padding: 24, textAlign: 'center', color: va.text3, fontSize: 12 }}>
                  Chưa có dữ liệu máy. MDC Agent chưa kết nối.
                </div>
              )}
              {machines.map(machine => {
                const isRunning = machine.runMode === 'START' || machine.runMode === 'ACTIVE'
                const isAlarm   = !!machine.alarmMessage
                const isOff     = !machine.lastSeen || new Date(machine.lastSeen).getTime() < Date.now() - 5 * 60_000
                const fg   = isAlarm ? va.err : isRunning ? va.active : va.text2
                const bg   = isAlarm ? va.errBg : isRunning ? va.activeBg : va.surface
                const label = isAlarm ? 'ALARM' : isRunning ? 'ĐANG CHẠY' : isOff ? 'OFF' : 'IDLE'
                return (
                  <div key={machine.machineCode} className="va-clickable" style={{ border: `1px solid ${va.border}`, borderLeft: `3px solid ${fg}`, borderRadius: 9, padding: '12px 13px', display: 'flex', flexDirection: 'column', gap: 9, background: isAlarm ? va.errBg : va.surface }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      <span style={{ width: 8, height: 8, borderRadius: '50%', background: fg, boxShadow: isRunning ? `0 0 0 3px ${fg}22` : 'none', flexShrink: 0 }} />
                      <span style={{ fontFamily: va.mono, fontWeight: 600, fontSize: 13, color: va.text }}>{machine.machineCode}</span>
                      <span style={{ fontSize: 11, color: va.text3, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1 }}>{machine.machineName ?? ''}</span>
                      <span style={{ fontSize: 9.5, fontWeight: 700, color: fg, background: bg, padding: '2px 7px', borderRadius: 4, letterSpacing: 0.4, textTransform: 'uppercase', flexShrink: 0 }}>{label}</span>
                    </div>
                    {isAlarm ? (
                      <div style={{ fontFamily: va.mono, fontSize: 11, color: va.err, fontWeight: 500 }}>⚠ {machine.alarmMessage}</div>
                    ) : isOff ? (
                      <div style={{ fontSize: 11, color: va.text3 }}>Máy tắt · ngoài ca</div>
                    ) : (
                      <div style={{ display: 'flex', gap: 13, fontFamily: va.mono, fontSize: 10.5, color: va.text2 }}>
                        {machine.spindleSpeed != null && <span>S {machine.spindleSpeed.toLocaleString()}</span>}
                        {machine.partCount    != null && <span style={{ marginLeft: 'auto' }}>Pcs <b style={{ color: va.text }}>{machine.partCount}</b></span>}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          </VACard>

          {/* Right column */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: 14, minHeight: 0 }}>
            {/* Production stats */}
            <VACard title="Tổng quan sản xuất" style={{ flex: 1 }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {[
                  ['Job đang chạy',          overview?.activeJobs,              va.accent  ],
                  ['Hoàn thành tháng này',   overview?.completedJobsThisMonth,  va.ok      ],
                  ['Tổng serials',           production?.serialsTotal,          va.text    ],
                  ['% tiến độ trung bình',   production ? `${production.avgProgressPercent}%` : '—', va.accent ],
                  ['Gage hết hạn / hỏng',    overview?.gagesExpiredOrDamaged,   va.err     ],
                  ['HC chờ duyệt',           overview?.pendingCalibRequests,    va.warn    ],
                ].map(([label, value, color]) => (
                  <div key={label as string} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12.5, paddingBottom: 8, borderBottom: `1px solid ${va.separator}` }}>
                    <span style={{ color: va.text2 }}>{label}</span>
                    <span style={{ fontFamily: va.mono, fontWeight: 600, color: color as string }}>{loading ? '…' : (value ?? '—')}</span>
                  </div>
                ))}
              </div>
            </VACard>

            {/* Quality summary */}
            <VACard title="Chất lượng (30 ngày)" style={{ flex: 1 }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {quality && (
                  <>
                    <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
                      <span style={{ fontFamily: va.mono, fontSize: 28, fontWeight: 700, color: quality.passRate >= 95 ? va.ok : quality.passRate >= 90 ? va.warn : va.err }}>{quality.passRate}%</span>
                      <span style={{ fontSize: 12, color: va.text3 }}>Pass rate</span>
                    </div>
                    <div style={{ height: 8, background: va.surface2, borderRadius: 4, overflow: 'hidden' }}>
                      <div style={{ height: '100%', width: `${quality.passRate}%`, background: quality.passRate >= 95 ? va.ok : quality.passRate >= 90 ? va.warn : va.err }} />
                    </div>
                    {[
                      ['Tổng đo kiểm',     quality.totalMeasured, va.text   ],
                      ['Pass',             quality.passCount,     va.ok     ],
                      ['Fail',             quality.failCount,     va.err    ],
                      ['NCR đang mở',      quality.openNcrs,      va.err    ],
                      ['NCR đóng tháng này', quality.closedNcrsThisMonth, va.ok ],
                    ].map(([label, value, color]) => (
                      <div key={label as string} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12 }}>
                        <span style={{ color: va.text2 }}>{label}</span>
                        <span style={{ fontFamily: va.mono, fontWeight: 600, color: color as string }}>{value}</span>
                      </div>
                    ))}
                  </>
                )}
                {!quality && !loading && <div style={{ color: va.text3, fontSize: 12 }}>Chưa có dữ liệu đo kiểm.</div>}
              </div>
            </VACard>
          </div>
        </div>
      </div>
    </div>
  )
}
