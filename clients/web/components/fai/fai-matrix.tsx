'use client'

import { useMemo, useState } from 'react'
import type { MouseEvent } from 'react'
import Link from 'next/link'
import { type FaiSheetDto, MEASURE_STAGE_LABELS } from '@/lib/api-client'
import { VABadge, VACard } from '@/components/va'
import { va } from '@/lib/va-tokens'

type Props = {
  sheet: FaiSheetDto
  stageFilter: string
}

const CATEGORY_COLOR: Record<string, string> = {
  LIN: va.primary, ANG: va.primaryLt, THD: va.accent, GEO: '#5D4037', SFC: va.text2,
}

type TooltipState = { x: number; y: number; lines: [string, string][] } | null

export function FaiMatrix({ sheet, stageFilter }: Props) {
  const { dimensions: dims, rows } = sheet
  const [tip, setTip] = useState<TooltipState>(null)

  const allCells = useMemo(() => rows.flatMap(r => r.cells), [rows])
  const totalCells = dims.length * rows.length

  const scopedCells = stageFilter === 'all'
    ? allCells
    : allCells.filter(c => c.measureStage === Number(stageFilter))

  const filledCells = scopedCells.filter(c => c.value != null).length
  const passCells = scopedCells.filter(c => c.result === 'Pass').length
  const failCells = scopedCells.filter(c => c.result === 'Fail').length
  const pendingCells = totalCells - filledCells
  const passRate = filledCells > 0 ? Math.round((passCells / filledCells) * 100) : null

  const lastMeasured = allCells
    .filter(c => c.measuredAt)
    .sort((a, b) => new Date(b.measuredAt!).getTime() - new Date(a.measuredAt!).getTime())[0]

  function showTip(e: MouseEvent, stageValue: { value: number | null; measuredByName: string | null; measureStage?: number | null; gageNo: string | null; hasNcr: boolean; ncrCode: string | null; measuredAt: string | null } | null, dim: { unit: string }) {
    if (!stageValue?.value && stageValue?.value !== 0) return
    setTip({
      x: e.clientX, y: e.clientY,
      lines: [
        ['Giá trị', `${stageValue.value}${dim.unit ? ' ' + dim.unit : ''}`],
        ['Người đo', stageValue.measuredByName ?? '—'],
        ['Stage', stageValue.measureStage != null ? MEASURE_STAGE_LABELS[stageValue.measureStage] : '—'],
        ['Gage', stageValue.gageNo ?? '—'],
        ...(stageValue.hasNcr ? [['NCR', stageValue.ncrCode ?? '⚑'] as [string, string]] : []),
        ['Lúc', stageValue.measuredAt ? new Date(stageValue.measuredAt).toLocaleString('vi-VN') : '—'],
      ],
    })
  }

  if (dims.length === 0) {
    return (
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
        Operation này chưa có dimension. Cần thêm dimensions trước.
      </div>
    )
  }

  return (
    <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 14 }}>
      {/* Info bar */}
      <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '14px 18px', display: 'flex', alignItems: 'center', boxShadow: va.shadow, flexWrap: 'wrap' }}>
        {([
          ['Part number', sheet.partNumber, true],
          ['Mô tả', sheet.partDescription, false],
          ['Rev', sheet.revCode, false],
          ['Job', sheet.jobNumber, false],
          ['Operation', sheet.partOpId === 0 ? 'Tất cả OP' : `OP${sheet.opNumber}`, false],
        ] as [string, string, boolean][]).map(([k, v, mono], i) => (
          <div key={k} style={{ display: 'flex', alignItems: 'center' }}>
            {i > 0 && <div style={{ height: 34, width: 1, background: va.separator, margin: '0 18px' }} />}
            <div>
              <div style={{ fontSize: 10, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 600, marginBottom: 3 }}>{k}</div>
              <div style={{ fontSize: 14, fontWeight: 600, fontFamily: mono ? va.mono : va.font, color: va.text }}>{v}</div>
            </div>
          </div>
        ))}
        {lastMeasured && (
          <div style={{ marginLeft: 'auto', textAlign: 'right' }}>
            <div style={{ fontSize: 10, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6, fontWeight: 600, marginBottom: 3 }}>Đo gần nhất</div>
            <div style={{ fontSize: 13, fontWeight: 600 }}>{lastMeasured.measuredByName ?? '—'}</div>
            <div style={{ fontSize: 10.5, color: va.text3 }}>{lastMeasured.measuredAt ? new Date(lastMeasured.measuredAt).toLocaleString('vi-VN') : '—'}</div>
          </div>
        )}
      </div>

      {/* Stats strip */}
      <div style={{ background: va.surface, border: `1px solid ${va.border}`, borderRadius: 11, padding: '15px 18px', display: 'flex', alignItems: 'center', gap: 24, boxShadow: va.shadow, flexWrap: 'wrap' }}>
        {([
          ['Tổng ô', totalCells, va.text],
          ['Đã đo', filledCells, va.primary],
          ['Pass', passCells, va.ok],
          ['Fail · NCR', failCells, va.err],
          ['Pending', pendingCells, va.text2],
          ['Pass rate', passRate != null ? `${passRate}%` : '—', va.accent],
        ] as [string, string | number, string][]).map(([label, value, color]) => (
          <div key={label}>
            <div style={{ fontFamily: va.mono, fontSize: 22, fontWeight: 600, color, lineHeight: 1 }}>{value}</div>
            <div style={{ fontSize: 10, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.5, fontWeight: 600, marginTop: 5 }}>{label}</div>
          </div>
        ))}
        <div style={{ marginLeft: 'auto' }}>
          <span style={{ fontSize: 11, color: va.text3 }}>
            Đang xem: <b style={{ color: va.text2 }}>{stageFilter === 'all' ? 'Tất cả' : MEASURE_STAGE_LABELS[Number(stageFilter)]}</b>
          </span>
        </div>
      </div>

      {/* Matrix card */}
      <VACard
        title="Serial × Dimension"
        sub={`${rows.length} serial × ${dims.length} balloon`}
        pad={false}
        style={{ minHeight: 0, flex: 1 }}
        right={<div style={{ display: 'flex', gap: 12, fontSize: 11, color: va.text2 }}>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ width: 9, height: 9, background: va.ok, borderRadius: 2 }} />Pass</span>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ width: 9, height: 9, background: va.err, borderRadius: 2 }} />Fail</span>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ width: 9, height: 9, background: va.borderStr, borderRadius: 2 }} />Chưa đo</span>
          <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}><span style={{ color: va.err, fontSize: 12 }}>⚑</span>NCR</span>
        </div>}
      >
        <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
          <table style={{ borderCollapse: 'separate', borderSpacing: 0, fontSize: 12, width: '100%' }}>
            <thead>
              <tr>
                <th style={{ position: 'sticky', left: 0, top: 0, background: va.surface2, padding: '8px 14px', textAlign: 'left', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderRight: `2px solid ${va.borderStr}`, borderBottom: `1px solid ${va.border}`, zIndex: 4, minWidth: 92 }}>Serial</th>
                {dims.map(d => {
                  const color = d.categoryCode ? CATEGORY_COLOR[d.categoryCode] ?? va.text2 : va.text2
                  return (
                    <th key={d.id} style={{ position: 'sticky', top: 0, background: va.surface2, padding: '8px 8px 9px', textAlign: 'center', borderBottom: `1px solid ${va.border}`, borderRight: `1px solid ${va.separator}`, minWidth: 96, verticalAlign: 'top', zIndex: 2 }}>
                      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4 }}>
                        <div style={{ width: 26, height: 26, borderRadius: '50%', border: `2px solid ${d.isCritical ? va.err : color}`, color: d.isCritical ? va.err : color, display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: va.mono, fontWeight: 700, fontSize: 11 }}>
                          {d.balloonNumber}
                        </div>
                        <div style={{ fontFamily: va.mono, fontSize: 10.5, fontWeight: 600, color: va.text, lineHeight: 1.3 }}>
                          {d.isTextType ? d.nominalText : `${d.nominalValue ?? ''} +${d.tolerancePlus ?? 0}/-${d.toleranceMinus ?? 0}`}
                        </div>
                        <div style={{ fontFamily: va.mono, fontSize: 8.5, color: va.text3, lineHeight: 1.25 }}>
                          {d.gageTypeCode ?? d.categoryCode ?? d.unit}
                        </div>
                        {d.opNumber && (
                          <div style={{ fontFamily: va.mono, fontSize: 8, color: va.text3, opacity: 0.7 }}>OP{d.opNumber}</div>
                        )}
                      </div>
                    </th>
                  )
                })}
                <th style={{ position: 'sticky', top: 0, right: 0, background: va.surface2, padding: '8px 14px', textAlign: 'center', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, borderLeft: `2px solid ${va.borderStr}`, zIndex: 3, minWidth: 88 }}>Kết quả</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(row => {
                const rowAllPass = stageFilter === 'all'
                  ? row.allPass
                  : row.cells.every(c => {
                      const sv = c.byStage[stageFilter]
                      return !sv || sv.result === 'Pass'
                    })
                return (
                  <tr key={row.productId} className="va-row">
                    <td style={{ position: 'sticky', left: 0, background: va.surface, padding: '0 14px', height: 46, borderRight: `2px solid ${va.borderStr}`, borderBottom: `1px solid ${va.separator}`, zIndex: 1 }}>
                      <Link href={`/fai/product/${row.productId}`} title="Xem toàn bộ dimension của Serial này qua mọi OP" style={{ color: va.primary, textDecoration: 'none', fontFamily: va.mono, fontWeight: 600 }}>
                        {row.serialNumber}
                      </Link>
                    </td>
                    {row.cells.map((cell, i) => {
                      const dim = dims[i]
                      const stageValue = stageFilter === 'all' ? cell : cell.byStage[stageFilter]
                      const value = stageValue?.value ?? null
                      const result = stageValue?.result ?? null
                      const hasData = stageFilter === 'all' || !!cell.byStage[stageFilter]
                      const bg = result === 'Pass' ? va.okBg : result === 'Fail' ? va.errBg : va.surface
                      return (
                        <td key={dim.id}
                          onMouseEnter={e => showTip(e, stageValue ? { ...stageValue, measureStage: stageFilter === 'all' ? cell.measureStage : Number(stageFilter) } : null, dim)}
                          onMouseMove={e => setTip(t => t ? { ...t, x: e.clientX, y: e.clientY } : t)}
                          onMouseLeave={() => setTip(null)}
                          style={{ position: 'relative', padding: 0, borderBottom: `1px solid ${va.separator}`, borderRight: `1px solid ${va.separator}`, background: bg, opacity: hasData ? 1 : 0.35 }}>
                          <div style={{ height: 46, padding: '0 10px', display: 'flex', alignItems: 'center', justifyContent: 'center', borderLeft: result === 'Fail' ? `2px solid ${va.err}` : '2px solid transparent' }}>
                            <span style={{ fontFamily: va.mono, fontSize: 12.5, fontWeight: result === 'Fail' ? 700 : 500, color: result === 'Fail' ? va.err : result === 'Pass' ? va.text : va.text3 }}>
                              {value ?? '—'}
                            </span>
                          </div>
                          {stageValue?.hasNcr && (
                            <span style={{ position: 'absolute', top: 2, right: 3, fontSize: 11, color: va.err, lineHeight: 1 }}>⚑</span>
                          )}
                        </td>
                      )
                    })}
                    <td style={{ position: 'sticky', right: 0, background: va.surface, padding: '0 14px', borderBottom: `1px solid ${va.separator}`, borderLeft: `2px solid ${va.borderStr}`, textAlign: 'center' }}>
                      <VABadge kind={rowAllPass ? 'ok' : 'err'} dot>{rowAllPass ? 'PASS' : 'FAIL'}</VABadge>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </VACard>

      <div style={{ fontSize: 11, color: va.text3, display: 'flex', alignItems: 'center', gap: 6 }}>
        <span style={{ color: va.text2 }}>ⓘ</span>
        View read-only — dữ liệu đo nhập từ Desktop app. Click số <b style={{ color: va.primary }}>SN</b> để mở Serial Measure Sheet (toàn bộ dimension của serial qua mọi OP).
      </div>

      {tip && (
        <div style={{ position: 'fixed', left: Math.min(tip.x + 14, window.innerWidth - 230), top: tip.y + 14, zIndex: 9999, pointerEvents: 'none', background: va.text, color: '#fff', borderRadius: 8, padding: '9px 11px', boxShadow: va.shadowLg, minWidth: 180, fontSize: 11.5 }}>
          {tip.lines.map(([k, val]) => (
            <div key={k} style={{ display: 'flex', justifyContent: 'space-between', gap: 14, padding: '1.5px 0' }}>
              <span style={{ color: 'rgba(255,255,255,0.6)' }}>{k}</span>
              <span style={{ fontFamily: va.mono, fontWeight: 500, color: k === 'NCR' ? '#FFB4A8' : '#fff' }}>{val}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
