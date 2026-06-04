'use client'

import { VATopbar, VAKpi, VACard, VABtn, VABadge } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'

const DOC_TYPES: Record<string, { label: string; color: string }> = {
  DRW: { label: 'Drawing',    color: '#6D3B1A' },
  GCD: { label: 'G-Code',     color: '#E65100' },
  RTC: { label: 'Route Card', color: '#5D4037' },
  FXT: { label: 'Fixture',    color: '#A0522D' },
  THD: { label: 'Thread Drw', color: '#F57C00' },
  TLS: { label: 'Tool List',  color: '#795548' },
  CAM: { label: 'CAM',        color: '#8D6E63' },
  CAD: { label: 'CAD',        color: '#6D4C41' },
}
const DOC_STATUS: Record<string, { label: string; kind: VaBadgeKind }> = {
  pending:  { label: 'Chờ duyệt', kind: 'warn' },
  approved: { label: 'Đã duyệt',  kind: 'ok'   },
  rejected: { label: 'Từ chối',   kind: 'err'  },
}
const DOCS = [
  { name: 'SHAFT-50H6_OP20_finish.nc',    type: 'GCD', part: 'SHAFT-50H6',  op: 'OP020', rev: 'B', status: 'pending',  by: 'Nguyễn V. Quân', at: '25/05 13:40', size: '142 KB' },
  { name: 'SHAFT-50H6_drawing.pdf',        type: 'DRW', part: 'SHAFT-50H6',  op: '—',     rev: 'B', status: 'approved', by: 'Nguyễn V. Quân', at: '24/05 09:12', size: '2.1 MB' },
  { name: 'SHAFT-50H6_OP20_route.pdf',     type: 'RTC', part: 'SHAFT-50H6',  op: 'OP020', rev: 'B', status: 'approved', by: 'Nguyễn V. Quân', at: '24/05 09:15', size: '380 KB' },
  { name: 'HOUSING-A12_OP30_fixture.dwg',  type: 'FXT', part: 'HOUSING-A12', op: 'OP030', rev: 'C', status: 'rejected', by: 'Trần Q. Bình',   at: '23/05 16:22', size: '1.4 MB', note: 'Thiếu dung sai kẹp' },
  { name: 'GEAR-Z48_OP40_tools.xlsx',      type: 'TLS', part: 'GEAR-Z48',    op: 'OP040', rev: 'D', status: 'pending',  by: 'Lê V. Sơn',      at: '25/05 11:08', size: '64 KB'  },
  { name: 'FLANGE-DN80_cad.step',           type: 'CAD', part: 'FLANGE-DN80', op: '—',     rev: 'A', status: 'approved', by: 'Nguyễn V. Quân', at: '20/05 14:30', size: '8.7 MB' },
  { name: 'SHAFT-50H6_OP10_rough.nc',      type: 'GCD', part: 'SHAFT-50H6',  op: 'OP010', rev: 'B', status: 'approved', by: 'Lê V. Sơn',      at: '22/05 08:50', size: '98 KB'  },
]

export default function DocumentsPage() {
  const pending = DOCS.filter(d => d.status === 'pending')

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, background: va.bg }}>
      <VATopbar title="Tài liệu kỹ thuật" breadcrumb="Hệ thống › Drawing · G-code · Route card"
        right={<><VABtn kind="ghost" style={{ marginRight: 8 }}>Hàng đợi duyệt ({pending.length})</VABtn><VABtn kind="primary">⬆ Upload</VABtn></>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Tổng tài liệu" value={DOCS.length} />
          <VAKpi label="Chờ duyệt"     value={pending.length} accent={va.warn} />
          <VAKpi label="Đã duyệt"      value={DOCS.filter(d => d.status === 'approved').length} accent={va.ok}  />
          <VAKpi label="Từ chối"        value={DOCS.filter(d => d.status === 'rejected').length} accent={va.err} />
        </div>

        {/* Pending banner */}
        {pending.length > 0 && (
          <div style={{ background: va.warnBg, border: `1px solid ${va.warn}44`, borderRadius: 11, padding: '14px 18px', display: 'flex', alignItems: 'center', gap: 14 }}>
            <div style={{ width: 34, height: 34, borderRadius: '50%', background: '#fff', color: va.warn, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 16, flexShrink: 0 }}>◷</div>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: va.text }}>{pending.length} tài liệu chờ Inspector duyệt</div>
              <div style={{ fontSize: 11.5, color: va.text2, marginTop: 1 }}>Chỉ file đã duyệt mới hiển thị trên MES tại máy</div>
            </div>
            <VABtn kind="accent">Duyệt ngay →</VABtn>
          </div>
        )}

        {/* Type legend */}
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {Object.entries(DOC_TYPES).map(([code, t]) => (
            <span key={code} className="va-clickable" style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '5px 10px', background: va.surface, border: `1px solid ${va.border}`, borderRadius: 7, fontSize: 11.5 }}>
              <span style={{ width: 8, height: 8, borderRadius: 2, background: t.color }} />
              <span style={{ fontFamily: va.mono, fontWeight: 700, color: t.color, fontSize: 10 }}>{code}</span>
              <span style={{ color: va.text2 }}>{t.label}</span>
            </span>
          ))}
        </div>

        {/* Doc table */}
        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
              <thead>
                <tr style={{ background: va.surface2 }}>
                  {['Tên file', 'Loại', 'Part / OP', 'Rev', 'Trạng thái', 'Người tạo', 'Kích thước', ''].map((h, i) => (
                    <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {DOCS.map((d, idx) => {
                  const t = DOC_TYPES[d.type]
                  return (
                    <tr key={idx} className="va-row va-clickable">
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
                          <span style={{ width: 26, height: 26, borderRadius: 6, background: t.color + '18', color: t.color, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, flexShrink: 0 }}>◰</span>
                          <span style={{ fontFamily: va.mono, fontSize: 12, color: va.text }}>{d.name}</span>
                        </div>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <span style={{ fontSize: 10, fontWeight: 700, color: t.color, background: va.surface2, padding: '2px 7px', borderRadius: 4, fontFamily: va.mono, border: `1px solid ${t.color}33` }}>{d.type}</span>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, fontSize: 11.5, color: va.text2 }}>{d.part}{d.op !== '—' ? ` · ${d.op}` : ''}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{d.rev}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                        <VABadge kind={DOC_STATUS[d.status].kind} dot={d.status === 'pending'}>{DOC_STATUS[d.status].label}</VABadge>
                        {d.note && <div style={{ fontSize: 10.5, color: va.err, marginTop: 3 }}>⚠ {d.note}</div>}
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>
                        <div style={{ fontSize: 12 }}>{d.by}</div>
                        <div style={{ fontSize: 10.5, color: va.text3, fontFamily: va.mono }}>{d.at}</div>
                      </td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, fontSize: 11.5 }}>{d.size}</td>
                      <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right', whiteSpace: 'nowrap' }}>
                        {d.status === 'pending'
                          ? <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                              <VABtn kind="ghost" style={{ height: 28, fontSize: 11, padding: '0 9px', color: va.err, borderColor: va.err + '55' }}>Từ chối</VABtn>
                              <VABtn kind="primary" style={{ height: 28, fontSize: 11, padding: '0 9px', background: va.ok, borderColor: va.ok }}>Duyệt</VABtn>
                            </div>
                          : <span className="va-clickable" style={{ fontSize: 11, color: va.primary, fontWeight: 600 }}>Xem</span>}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </VACard>
      </div>
    </div>
  )
}
