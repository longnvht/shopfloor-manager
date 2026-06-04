'use client'

import { useState, useEffect, useCallback } from 'react'
import { api } from '@/lib/api-client'
import { VATopbar, VACard, VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'
import type { VaBadgeKind } from '@/lib/va-tokens'

type RunMode = 'START' | 'RESET' | 'ACTIVE' | null
type MachineStatus = {
  machineId: number; machineCode: string; machineName: string | null
  runMode: string | null; tmMode: string | null; alarmMessage: string | null
  spindleSpeed: number | null; spindleLoad: number | null; feedrate: number | null
  xPosition: number | null; yPosition: number | null; zPosition: number | null
  program: string | null; partCount: number | null; lastSeen: string
}

function statusMeta(m: MachineStatus): { fg: string; bg: string; label: string; kind: VaBadgeKind } {
  if (m.alarmMessage) return { fg: va.err,    bg: va.errBg,    label: 'Alarm',      kind: 'err'     }
  if (m.runMode === 'START' || m.runMode === 'ACTIVE')
                          return { fg: va.active, bg: va.activeBg, label: 'Đang chạy',  kind: 'running' }
  if (!m.lastSeen || new Date(m.lastSeen).getTime() < Date.now() - 5 * 60_000)
                          return { fg: va.text3,  bg: '#F0EDE8',   label: 'Off',         kind: 'neutral' }
  return                         { fg: va.text2,  bg: va.surface2, label: 'Idle',        kind: 'neutral' }
}

export default function CncPage() {
  const [machines, setMachines] = useState<MachineStatus[]>([])
  const [sel, setSel]           = useState<string | null>(null)
  const [loading, setLoading]   = useState(true)

  const load = useCallback(async () => {
    const res = await api.machines.status()
    if (res.success && res.data) {
      const list = res.data as MachineStatus[]
      setMachines(list)
      if (!sel && list.length > 0) setSel(list[0].machineCode)
    }
    setLoading(false)
  }, [])

  useEffect(() => {
    load()
    // Polling every 5s — SignalR hookup can be added later
    const t = setInterval(load, 5_000)
    return () => clearInterval(t)
  }, [load])

  const selMachine = machines.find(m => m.machineCode === sel)

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="CNC Live" breadcrumb="Sản xuất › Giám sát máy real-time"
        right={
          <span style={{ display: 'flex', alignItems: 'center', gap: 7, fontSize: 12, color: va.ok, fontWeight: 600, padding: '6px 12px', background: va.okBg, borderRadius: 7 }}>
            <span style={{ width: 7, height: 7, borderRadius: '50%', background: va.ok, boxShadow: `0 0 0 3px ${va.ok}22` }} />
            MQTT · Mosquitto 1883
          </span>
        }
      />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* Machine list */}
        <div className="va-scroll" style={{ width: 200, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0, padding: 10 }}>
          {loading && <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Đang tải…</div>}
          {machines.map(m => {
            const s = statusMeta(m)
            const on = m.machineCode === sel
            return (
              <div key={m.machineCode} className="va-clickable" onClick={() => setSel(m.machineCode)}
                style={{ padding: '11px 12px', borderRadius: 9, marginBottom: 6, border: `1px solid ${on ? va.accent : va.border}`, background: on ? va.accentBg : va.surface, display: 'flex', alignItems: 'center', gap: 9 }}>
                <span style={{ width: 9, height: 9, borderRadius: '50%', background: s.fg, boxShadow: s.label === 'Đang chạy' ? `0 0 0 3px ${s.fg}22` : 'none', flexShrink: 0 }} />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: 600, color: va.text }}>{m.machineCode}</div>
                  <div style={{ fontSize: 10, color: s.fg, fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.3 }}>{s.label}</div>
                </div>
              </div>
            )
          })}
          {!loading && machines.length === 0 && (
            <div style={{ padding: 16, fontSize: 12, color: va.text3, textAlign: 'center' }}>
              Chưa có dữ liệu.<br/>MDC Agent chưa connect.
            </div>
          )}
        </div>

        {/* Detail */}
        {selMachine ? (
          <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 16 }}>
            {(() => {
              const s = statusMeta(selMachine)
              return (
                <>
                  {/* Header */}
                  <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                    <div style={{ width: 52, height: 52, borderRadius: 12, background: s.bg, color: s.fg, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 24, flexShrink: 0 }}>◈</div>
                    <div style={{ flex: 1 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                        <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{selMachine.machineCode}</h2>
                        <VABadge kind={s.kind} dot={s.label === 'Đang chạy'}>{s.label.toUpperCase()}</VABadge>
                      </div>
                      <div style={{ fontSize: 12.5, color: va.text2, marginTop: 4 }}>{selMachine.machineName ?? ''}</div>
                    </div>
                    <div style={{ display: 'flex', gap: 8 }}>
                      {([['TM', selMachine.tmMode], ['RUN', selMachine.runMode], ['Prog', selMachine.program]] as [string, string | null][]).map(([k, v]) => v && (
                        <div key={k} style={{ padding: '6px 12px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 7, textAlign: 'center' }}>
                          <div style={{ fontSize: 9, color: va.text3, textTransform: 'uppercase', letterSpacing: 0.5 }}>{k}</div>
                          <div style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: 600, color: va.text }}>{v}</div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Alarm */}
                  {selMachine.alarmMessage && (
                    <div style={{ background: va.errBg, border: `1px solid ${va.err}44`, borderRadius: 9, padding: '12px 16px', fontFamily: va.mono, fontSize: 13, color: va.err, fontWeight: 600 }}>
                      ⚠ {selMachine.alarmMessage}
                    </div>
                  )}

                  {/* Gauges */}
                  <div style={{ display: 'flex', gap: 13 }}>
                    {([
                      ['Spindle Speed', selMachine.spindleSpeed, 'rpm', 3000, va.accent],
                      ['Spindle Load',  selMachine.spindleLoad,  '%',   100,  va.active],
                      ['Feedrate',      selMachine.feedrate,     'mm/min', 2000, va.primary],
                      ['Part Count',    selMachine.partCount,    'pcs', null, va.ok],
                    ] as [string, number | null, string, number | null, string][]).map(([label, value, unit, max, color]) => (
                      <div key={label} style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '14px 16px', flex: 1, boxShadow: va.shadow }}>
                        <div style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 600 }}>{label}</div>
                        <div style={{ display: 'flex', alignItems: 'baseline', gap: 5, margin: '6px 0 9px' }}>
                          <span style={{ fontFamily: va.mono, fontSize: 26, fontWeight: 600, color, lineHeight: 1 }}>{value ?? '—'}</span>
                          <span style={{ fontSize: 12, color: va.text3 }}>{unit}</span>
                        </div>
                        {max && value != null && (
                          <div style={{ height: 6, background: va.surface2, borderRadius: 3, overflow: 'hidden' }}>
                            <div style={{ height: '100%', width: `${Math.min(100, value / max * 100)}%`, background: color }} />
                          </div>
                        )}
                      </div>
                    ))}
                  </div>

                  {/* Position */}
                  {(selMachine.xPosition != null || selMachine.yPosition != null || selMachine.zPosition != null) && (
                    <VACard title="Tọa độ trục">
                      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                        {([['X', selMachine.xPosition], ['Y', selMachine.yPosition], ['Z', selMachine.zPosition]] as [string, number | null][]).map(([ax, v]) => v != null && (
                          <div key={ax} style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '12px 16px', background: va.text, borderRadius: 9 }}>
                            <span style={{ fontFamily: va.mono, fontSize: 15, fontWeight: 700, color: va.accent, width: 20 }}>{ax}</span>
                            <span style={{ fontFamily: va.mono, fontSize: 22, fontWeight: 600, color: '#FF8C00', marginLeft: 'auto', letterSpacing: 0.5 }}>{v.toFixed(3)}</span>
                            <span style={{ fontSize: 11, color: 'rgba(255,255,255,0.5)' }}>mm</span>
                          </div>
                        ))}
                      </div>
                    </VACard>
                  )}

                  <div style={{ fontSize: 11, color: va.text3, fontFamily: va.mono }}>
                    Cập nhật lần cuối: {selMachine.lastSeen !== '0001-01-01T00:00:00+00:00'
                      ? new Date(selMachine.lastSeen).toLocaleString('vi-VN')
                      : 'Chưa nhận dữ liệu'}
                  </div>
                </>
              )
            })()}
          </div>
        ) : (
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13, flexDirection: 'column', gap: 8 }}>
            <div style={{ fontSize: 32 }}>◈</div>
            <div>Chọn máy để xem trạng thái</div>
            <div style={{ fontSize: 11 }}>Dữ liệu real-time từ MDC Agent qua MQTT</div>
          </div>
        )}
      </div>
    </div>
  )
}
