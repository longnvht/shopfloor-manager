'use client'

import { useState } from 'react'
import { VATopbar, VACard, VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'

type MachineStatus = 'running' | 'idle' | 'alarm' | 'setup' | 'off'
const STATUS_META: Record<MachineStatus, { fg: string; bg: string; label: string }> = {
  running: { fg: va.active, bg: va.activeBg, label: 'Đang chạy' },
  idle:    { fg: va.text2,  bg: va.surface2, label: 'Idle'       },
  alarm:   { fg: va.err,    bg: va.errBg,    label: 'Alarm'      },
  setup:   { fg: va.warn,   bg: va.warnBg,   label: 'Setup'      },
  off:     { fg: va.text3,  bg: '#F0EDE8',   label: 'Off'        },
}
const MACHINES = [
  { code: 'MC-01', name: 'Mazak QT-200',  status: 'running' as MachineStatus },
  { code: 'MC-02', name: 'DMG NEF-400',   status: 'running' as MachineStatus },
  { code: 'MC-03', name: 'Doosan PUMA',   status: 'idle'    as MachineStatus },
  { code: 'MC-04', name: 'Mori Seiki',    status: 'alarm'   as MachineStatus },
  { code: 'MC-05', name: 'Mazak VTC',     status: 'running' as MachineStatus },
  { code: 'MC-06', name: 'Haas VF-2',     status: 'setup'   as MachineStatus },
  { code: 'MC-07', name: 'Okuma Lathe',   status: 'running' as MachineStatus },
  { code: 'MC-08', name: 'Citizen L20',   status: 'off'     as MachineStatus },
]
const DETAIL = {
  machine: 'MC-01', name: 'Mazak QT-200 · CNC Turning',
  tmMode: 'AUTO', atMode: 'MEMORY', runMode: 'START', tool: 3,
  spindleSpeed: 2480, spindleLoad: 62, feedrate: 820, feedOverride: 100,
  pos: { x: 125.430, y: 0.000, z: -45.200 },
  partCount: 18, partTarget: 27,
  job: 'JB-26-031', op: 'OP020 · CNC Turning', operator: 'Phạm V. Hùng',
  loadHist:  [40,44,52,61,58,63,66,62,55,60,64,68,62,59,57,61,65,70,66,62,58,55,60,63,67,62,59,61,64,62],
  feedHist:  [800,810,820,815,800,790,820,830,825,810,800,815,820,818,805,800,810,825,830,820,815,808,800,812,820,825,818,810,815,820],
  events: [
    { t: '14:18:42', from: 'IDLE',    to: 'RUNNING', note: 'Bắt đầu OP020' },
    { t: '12:02:10', from: 'RUNNING', to: 'IDLE',    note: 'Nghỉ giữa ca'  },
    { t: '11:48:33', from: 'ALARM',   to: 'RUNNING', note: 'Reset alarm OT' },
    { t: '11:45:01', from: 'RUNNING', to: 'ALARM',   note: 'PS0341 LIMIT'  },
    { t: '08:14:05', from: 'OFF',     to: 'RUNNING', note: 'Khởi động ca 1' },
  ],
}

function Spark({ data, w, h, color, fillColor }: { data: number[]; w: number; h: number; color: string; fillColor: string }) {
  const max = Math.max(...data) * 1.1
  const min = Math.min(...data) * 0.9
  const pts = data.map((v, i) => [i / (data.length - 1) * w, h - ((v - min) / (max - min)) * h] as [number, number])
  const line = pts.map((p, i) => `${i ? 'L' : 'M'}${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join(' ')
  const area = `${line} L${w} ${h} L0 ${h} Z`
  return (
    <svg width={w} height={h} style={{ display: 'block' }}>
      <path d={area} fill={fillColor} />
      <path d={line} fill="none" stroke={color} strokeWidth="2" />
    </svg>
  )
}

function Gauge({ label, value, unit, pct, color }: { label: string; value: number | string; unit: string; pct: number; color: string }) {
  return (
    <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '14px 16px', flex: 1, boxShadow: va.shadow }}>
      <div style={{ fontSize: 10.5, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 600 }}>{label}</div>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 5, margin: '6px 0 9px' }}>
        <span style={{ fontFamily: va.mono, fontSize: 26, fontWeight: 600, color, lineHeight: 1 }}>{value}</span>
        <span style={{ fontSize: 12, color: va.text3 }}>{unit}</span>
      </div>
      <div style={{ height: 6, background: va.surface2, borderRadius: 3, overflow: 'hidden' }}>
        <div style={{ height: '100%', width: `${pct}%`, background: color }} />
      </div>
    </div>
  )
}

const toColor: Record<string, string> = { RUNNING: va.active, IDLE: va.text2, ALARM: va.err, OFF: va.text3 }

export default function CncPage() {
  const [sel, setSel] = useState('MC-01')
  const C = DETAIL

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="CNC Live" breadcrumb="Sản xuất › Giám sát máy real-time"
        right={<span style={{ display: 'flex', alignItems: 'center', gap: 7, fontSize: 12, color: va.ok, fontWeight: 600, padding: '6px 12px', background: va.okBg, borderRadius: 7 }}>
          <span style={{ width: 7, height: 7, borderRadius: '50%', background: va.ok, boxShadow: `0 0 0 3px ${va.ok}22` }} />MQTT · Mosquitto 1883
        </span>} />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* Machine list */}
        <div className="va-scroll" style={{ width: 200, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0, padding: 10 }}>
          {MACHINES.map(m => {
            const s = STATUS_META[m.status]
            const on = m.code === sel
            return (
              <div key={m.code} className="va-clickable" onClick={() => setSel(m.code)}
                style={{ padding: '11px 12px', borderRadius: 9, marginBottom: 6, border: `1px solid ${on ? va.accent : va.border}`, background: on ? va.accentBg : va.surface, display: 'flex', alignItems: 'center', gap: 9 }}>
                <span style={{ width: 9, height: 9, borderRadius: '50%', background: s.fg, boxShadow: m.status === 'running' ? `0 0 0 3px ${s.fg}22` : 'none', flexShrink: 0 }} />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: 600, color: va.text }}>{m.code}</div>
                  <div style={{ fontSize: 10, color: s.fg, fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.3 }}>{s.label}</div>
                </div>
              </div>
            )
          })}
        </div>

        {/* Detail */}
        <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 16 }}>
          {/* Header */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
            <div style={{ width: 52, height: 52, borderRadius: 12, background: va.activeBg, color: va.active, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 24, flexShrink: 0 }}>◈</div>
            <div style={{ flex: 1 }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <h2 style={{ margin: 0, fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.text }}>{C.machine}</h2>
                <VABadge kind="running" dot>ĐANG CHẠY</VABadge>
              </div>
              <div style={{ fontSize: 12.5, color: va.text2, marginTop: 4 }}>{C.name} · {C.job} · {C.op} · {C.operator}</div>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              {([['TM', C.tmMode], ['AT', C.atMode], ['RUN', C.runMode], ['Tool', 'T' + C.tool]] as [string, string | number][]).map(([k, v]) => (
                <div key={k} style={{ padding: '6px 12px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 7, textAlign: 'center' }}>
                  <div style={{ fontSize: 9, color: va.text3, textTransform: 'uppercase', letterSpacing: 0.5 }}>{k}</div>
                  <div style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: 600, color: va.text }}>{v}</div>
                </div>
              ))}
            </div>
          </div>

          {/* Gauges */}
          <div style={{ display: 'flex', gap: 13 }}>
            <Gauge label="Spindle Speed" value={C.spindleSpeed.toLocaleString()} unit="rpm"    pct={C.spindleLoad}                    color={va.accent}  />
            <Gauge label="Spindle Load"  value={C.spindleLoad}                   unit="%"      pct={C.spindleLoad}                    color={C.spindleLoad > 80 ? va.err : va.active} />
            <Gauge label="Feedrate"      value={C.feedrate}                      unit="mm/min" pct={C.feedOverride}                   color={va.primary} />
            <Gauge label="Part Count"    value={C.partCount}                     unit={`/ ${C.partTarget}`} pct={C.partCount / C.partTarget * 100} color={va.ok} />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            <VACard title="Spindle Load" sub="30 mẫu gần nhất">
              <div style={{ display: 'flex', alignItems: 'flex-end', gap: 12 }}>
                <div style={{ fontFamily: va.mono, fontSize: 30, fontWeight: 600, color: va.active }}>{C.spindleLoad}<span style={{ fontSize: 14, color: va.text3 }}>%</span></div>
                <div style={{ flex: 1 }}><Spark data={C.loadHist} w={320} h={64} color={va.active} fillColor={va.activeBg} /></div>
              </div>
            </VACard>
            <VACard title="Feedrate" sub="30 mẫu gần nhất">
              <div style={{ display: 'flex', alignItems: 'flex-end', gap: 12 }}>
                <div style={{ fontFamily: va.mono, fontSize: 30, fontWeight: 600, color: va.primary }}>{C.feedrate}<span style={{ fontSize: 14, color: va.text3 }}> mm/min</span></div>
                <div style={{ flex: 1 }}><Spark data={C.feedHist} w={320} h={64} color={va.primary} fillColor={va.accentBg} /></div>
              </div>
            </VACard>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1.4fr', gap: 14 }}>
            <VACard title="Tọa độ trục">
              <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                {([['X', C.pos.x], ['Y', C.pos.y], ['Z', C.pos.z]] as [string, number][]).map(([ax, v]) => (
                  <div key={ax} style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '12px 16px', background: va.text, borderRadius: 9 }}>
                    <span style={{ fontFamily: va.mono, fontSize: 15, fontWeight: 700, color: va.accent, width: 20 }}>{ax}</span>
                    <span style={{ fontFamily: va.mono, fontSize: 22, fontWeight: 600, color: '#FF8C00', marginLeft: 'auto', letterSpacing: 0.5 }}>{v.toFixed(3)}</span>
                    <span style={{ fontSize: 11, color: 'rgba(255,255,255,0.5)' }}>mm</span>
                  </div>
                ))}
              </div>
            </VACard>
            <VACard title="Machine Events" sub="thay đổi trạng thái" pad={false}>
              <div className="va-scroll" style={{ overflow: 'auto', maxHeight: 230 }}>
                {C.events.map((e, i) => (
                  <div key={i} className="va-row" style={{ padding: '11px 16px', borderBottom: i < C.events.length - 1 ? `1px solid ${va.separator}` : 'none', display: 'flex', alignItems: 'center', gap: 10 }}>
                    <span style={{ fontFamily: va.mono, fontSize: 11, color: va.text3, width: 64 }}>{e.t}</span>
                    <span style={{ fontFamily: va.mono, fontSize: 11, color: va.text3 }}>{e.from}</span>
                    <span style={{ color: va.text3 }}>→</span>
                    <span style={{ fontFamily: va.mono, fontSize: 11.5, fontWeight: 700, color: toColor[e.to] ?? va.text }}>{e.to}</span>
                    <span style={{ marginLeft: 'auto', fontSize: 11, color: va.text2 }}>{e.note}</span>
                  </div>
                ))}
              </div>
            </VACard>
          </div>
        </div>
      </div>
    </div>
  )
}
