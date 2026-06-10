'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type CalibRequestDto, type CalibVendorDto, type GageDto } from '@/lib/api-client'
import { VATopbar, VAKpi, VACard, VABtn, VABadge } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { useAuthStore } from '@/stores/auth.store'

const STATUS_META: Record<number, { label: string; kind: VaBadgeKind }> = {
  0: { label: 'Chờ duyệt',  kind: 'warn'    },
  1: { label: 'Đã gửi',     kind: 'running' },
  2: { label: 'Hoàn thành', kind: 'ok'      },
  3: { label: 'Đã hủy',     kind: 'neutral' },
}

// ── Complete calibration modal ─────────────────────────────────────────────
function CompleteModal({ request, onClose, onDone }: { request: CalibRequestDto; onClose: () => void; onDone: () => void }) {
  const { user } = useAuthStore()
  const [form, setForm] = useState({
    calibratedBy: user?.name ?? '',
    calibrationDate: new Date().toISOString().split('T')[0],
    asFoundConditions: 'Pass',
    adjustmentMade: '',
    temperature: '',
    humidity: '',
  })
  const [saving, setSaving] = useState(false)

  async function handleSubmit() {
    if (!form.calibratedBy || !form.calibrationDate) {
      alert('Vui lòng điền đầy đủ thông tin')
      return
    }
    setSaving(true)
    const res = await api.calibration.complete({
      requestId:          request.id,
      calibratedBy:       form.calibratedBy,
      calibrationDate:    form.calibrationDate,
      asFoundConditions:  form.asFoundConditions,
      adjustmentMade:     form.adjustmentMade ? Number(form.adjustmentMade) : undefined,
      temperature:        form.temperature   ? Number(form.temperature)    : undefined,
      humidity:           form.humidity      ? Number(form.humidity)       : undefined,
    })
    if (res.success) { onDone() }
    else alert(res.error ?? 'Lỗi nhập kết quả')
    setSaving(false)
  }

  const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50 }
  const box: React.CSSProperties     = { background: va.surface, borderRadius: 12, padding: 24, width: 480, maxHeight: '90vh', overflow: 'auto', boxShadow: va.shadowLg }
  const field: React.CSSProperties   = { display: 'flex', flexDirection: 'column', gap: 4, marginBottom: 14 }
  const lbl: React.CSSProperties     = { fontSize: 11.5, fontWeight: 600, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.5 }
  const inp: React.CSSProperties     = { padding: '9px 12px', borderRadius: 7, border: `1px solid ${va.border}`, fontSize: 13, fontFamily: va.font, outline: 'none' }

  return (
    <div style={overlay} onClick={e => e.target === e.currentTarget && onClose()}>
      <div style={box}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 18 }}>
          <div>
            <div style={{ fontSize: 16, fontWeight: 700, color: va.text }}>Nhập kết quả hiệu chuẩn</div>
            <div style={{ fontSize: 12, color: va.text2, marginTop: 2 }}>{request.gageNo} — {request.gageDescription}</div>
          </div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 18, color: va.text3 }}>✕</button>
        </div>

        <div style={field}>
          <label style={lbl}>Người thực hiện *</label>
          <input style={inp} value={form.calibratedBy} onChange={e => setForm(f => ({ ...f, calibratedBy: e.target.value }))} />
        </div>
        <div style={field}>
          <label style={lbl}>Ngày hiệu chuẩn *</label>
          <input type="date" style={inp} value={form.calibrationDate} onChange={e => setForm(f => ({ ...f, calibrationDate: e.target.value }))} />
        </div>
        <div style={field}>
          <label style={lbl}>Tình trạng trước hiệu chuẩn</label>
          <select style={inp} value={form.asFoundConditions} onChange={e => setForm(f => ({ ...f, asFoundConditions: e.target.value }))}>
            <option>Pass</option>
            <option>Fail</option>
            <option>Out of tolerance</option>
          </select>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 10, marginBottom: 14 }}>
          {[
            ['Hiệu chỉnh (mm)', 'adjustmentMade'],
            ['Nhiệt độ (°C)',    'temperature'   ],
            ['Độ ẩm (%)',        'humidity'      ],
          ].map(([label, key]) => (
            <div key={key} style={field}>
              <label style={lbl}>{label}</label>
              <input type="number" step="0.01" style={inp} value={(form as any)[key]} onChange={e => setForm(f => ({ ...f, [key]: e.target.value }))} />
            </div>
          ))}
        </div>

        <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <VABtn kind="ghost" onClick={onClose} disabled={saving}>Hủy</VABtn>
          <VABtn kind="primary" style={{ background: va.ok, borderColor: va.ok }} onClick={handleSubmit} disabled={saving}>
            {saving ? 'Đang lưu…' : '✓ Hoàn thành hiệu chuẩn'}
          </VABtn>
        </div>
      </div>
    </div>
  )
}

// ── Create request modal ───────────────────────────────────────────────────
function CreateRequestModal({ dueGages, vendors, onClose, onDone }: { dueGages: GageDto[]; vendors: CalibVendorDto[]; onClose: () => void; onDone: () => void }) {
  const { user } = useAuthStore()
  const [gageId,   setGageId]   = useState('')
  const [vendorId, setVendorId] = useState('')
  const [saving,   setSaving]   = useState(false)

  async function handleSubmit() {
    if (!gageId) { alert('Chọn dụng cụ cần hiệu chuẩn'); return }
    setSaving(true)
    const res = await api.calibration.createRequest({
      gageId:  Number(gageId),
      vendorId: vendorId ? Number(vendorId) : undefined,
    })
    if (res.success) { onDone() }
    else alert(res.error ?? 'Lỗi tạo yêu cầu')
    setSaving(false)
  }

  const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 50 }
  const box: React.CSSProperties     = { background: va.surface, borderRadius: 12, padding: 24, width: 420, boxShadow: va.shadowLg }
  const field: React.CSSProperties   = { display: 'flex', flexDirection: 'column', gap: 4, marginBottom: 14 }
  const lbl: React.CSSProperties     = { fontSize: 11.5, fontWeight: 600, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.5 }
  const sel: React.CSSProperties     = { padding: '9px 12px', borderRadius: 7, border: `1px solid ${va.border}`, fontSize: 13, fontFamily: va.font, outline: 'none' }

  return (
    <div style={overlay} onClick={e => e.target === e.currentTarget && onClose()}>
      <div style={box}>
        <div style={{ fontSize: 16, fontWeight: 700, color: va.text, marginBottom: 18 }}>Tạo yêu cầu hiệu chuẩn</div>

        <div style={field}>
          <label style={lbl}>Dụng cụ đo *</label>
          <select style={sel} value={gageId} onChange={e => setGageId(e.target.value)}>
            <option value="">— Chọn gage —</option>
            {dueGages.map(g => (
              <option key={g.id} value={g.id}>{g.gageNo} · {g.description}</option>
            ))}
          </select>
        </div>
        <div style={field}>
          <label style={lbl}>Vendor hiệu chuẩn</label>
          <select style={sel} value={vendorId} onChange={e => setVendorId(e.target.value)}>
            <option value="">— Không chỉ định —</option>
            {vendors.map(v => <option key={v.id} value={v.id}>{v.name}</option>)}
          </select>
        </div>

        <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <VABtn kind="ghost" onClick={onClose} disabled={saving}>Hủy</VABtn>
          <VABtn kind="primary" onClick={handleSubmit} disabled={saving}>
            {saving ? 'Đang tạo…' : '+ Tạo yêu cầu'}
          </VABtn>
        </div>
      </div>
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────
export default function CalibrationPage() {
  const [requests,  setRequests]  = useState<CalibRequestDto[]>([])
  const [vendors,   setVendors]   = useState<CalibVendorDto[]>([])
  const [dueGages,  setDueGages]  = useState<GageDto[]>([])
  const [loading,   setLoading]   = useState(true)
  const [completeTarget, setCompleteTarget] = useState<CalibRequestDto | null>(null)
  const [showCreate,     setShowCreate]     = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    const [rRes, vRes, dRes] = await Promise.all([
      api.calibration.requests(),
      api.calibration.vendors(),
      api.gages.calibDue(60),
    ])
    if (rRes.success && rRes.data) setRequests(rRes.data)
    if (vRes.success && vRes.data) setVendors(vRes.data)
    if (dRes.success && dRes.data) setDueGages(dRes.data)
    setLoading(false)
  }, [])

  useEffect(() => { load() }, [load])

  async function handleApprove(id: number) {
    const res = await api.calibration.approveRequest(id)
    if (res.success) load()
    else alert(res.error ?? 'Lỗi duyệt yêu cầu')
  }

  const pending   = requests.filter(r => r.status === 0).length
  const approved  = requests.filter(r => r.status === 1).length
  const completed = requests.filter(r => r.status === 2).length

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar title="Hiệu chuẩn (Calibration)" breadcrumb="Chất lượng › Chu trình hiệu chuẩn"
        right={<VABtn kind="primary" onClick={() => setShowCreate(true)}>+ Tạo yêu cầu</VABtn>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div style={{ display: 'flex', gap: 13 }}>
          <VAKpi label="Chờ duyệt"         value={pending}        accent={va.warn}   />
          <VAKpi label="Đang hiệu chuẩn"    value={approved}       accent={va.active} />
          <VAKpi label="Hoàn thành (tháng)" value={completed}      accent={va.ok}     />
          <VAKpi label="Sắp đến hạn (60d)"  value={dueGages.length} />
          <VAKpi label="Vendor"             value={vendors.length}  />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1.6fr 1fr', gap: 14, flex: 1, minHeight: 0 }}>
          <VACard title="Yêu cầu hiệu chuẩn" sub={`${requests.length} yêu cầu`} pad={false} style={{ minHeight: 0 }}>
            <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
              {loading ? (
                <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Đang tải…</div>
              ) : (
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                  <thead>
                    <tr style={{ background: va.surface2 }}>
                      {['Gage', 'Vendor', 'Ngày YC', 'Trạng thái', ''].map((h, i) => (
                        <th key={i} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {requests.map(r => {
                      const sm = STATUS_META[r.status] ?? { label: String(r.status), kind: 'neutral' as VaBadgeKind }
                      return (
                        <tr key={r.id} className="va-row va-clickable">
                          <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                            <div style={{ fontFamily: va.mono, fontWeight: 600, color: va.text }}>{r.gageNo}</div>
                            <div style={{ fontSize: 11, color: va.text2 }}>{r.gageDescription}</div>
                          </td>
                          <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{r.vendorName ?? '—'}</td>
                          <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2 }}>{r.requestDate}</td>
                          <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }}>
                            <VABadge kind={sm.kind} dot={r.status !== 2}>{sm.label}</VABadge>
                            {r.calibrationDate && <span style={{ fontSize: 10.5, color: va.ok, marginLeft: 6, fontWeight: 600 }}>{r.calibrationDate}</span>}
                          </td>
                          <td style={{ padding: '11px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'right', whiteSpace: 'nowrap' }}>
                            {r.status === 0 && (
                              <VABtn kind="primary" style={{ height: 28, fontSize: 11, padding: '0 10px' }} onClick={() => handleApprove(r.id)}>Duyệt</VABtn>
                            )}
                            {r.status === 1 && (
                              <VABtn kind="accent" style={{ height: 28, fontSize: 11, padding: '0 10px' }} onClick={() => setCompleteTarget(r)}>Nhận KQ</VABtn>
                            )}
                            {r.status === 2 && (
                              <span className="va-clickable" style={{ fontSize: 11, color: va.primary, fontWeight: 600 }}>⬇ Chứng chỉ</span>
                            )}
                          </td>
                        </tr>
                      )
                    })}
                    {requests.length === 0 && (
                      <tr><td colSpan={5} style={{ padding: 24, textAlign: 'center', color: va.text3, fontSize: 12 }}>Chưa có yêu cầu.</td></tr>
                    )}
                  </tbody>
                </table>
              )}
            </div>
          </VACard>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 14, minHeight: 0 }}>
            <VACard title="Sắp đến hạn" sub="60 ngày tới" pad={false} style={{ flex: 1, minHeight: 0 }}>
              <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
                {dueGages.length === 0
                  ? <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Không có gage nào sắp hết hạn.</div>
                  : dueGages.map(g => (
                    <div key={g.id} className="va-row va-clickable" style={{ padding: '12px 16px', borderBottom: `1px solid ${va.separator}`, display: 'flex', alignItems: 'center', gap: 10 }}>
                      <div style={{ flex: 1 }}>
                        <div style={{ fontFamily: va.mono, fontWeight: 600, fontSize: 12.5, color: va.text }}>{g.gageNo}</div>
                        <div style={{ fontSize: 11, color: va.text2 }}>{g.description}</div>
                      </div>
                      <div style={{ textAlign: 'right' }}>
                        <div style={{ fontFamily: va.mono, fontSize: 13, fontWeight: 600, color: (g.daysRemaining ?? 999) <= 30 ? va.warn : va.text2 }}>
                          {g.daysRemaining != null ? `${g.daysRemaining}d` : '—'}
                        </div>
                        {g.dueDate && <div style={{ fontSize: 10, color: va.text3, fontFamily: va.mono }}>{g.dueDate}</div>}
                      </div>
                    </div>
                  ))
                }
              </div>
            </VACard>

            <VACard title="Vendor hiệu chuẩn" pad={false}>
              {vendors.length === 0
                ? <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>Chưa có vendor.</div>
                : vendors.map((v, i) => (
                  <div key={v.id} style={{ padding: '12px 16px', borderBottom: i < vendors.length - 1 ? `1px solid ${va.separator}` : 'none', display: 'flex', alignItems: 'center', gap: 10 }}>
                    <div style={{ width: 34, height: 34, borderRadius: 8, background: va.accentLt, color: va.primary, display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 13, flexShrink: 0 }}>{v.name[0]}</div>
                    <div style={{ flex: 1 }}>
                      <div style={{ fontSize: 12.5, fontWeight: 600, color: va.text }}>{v.name}</div>
                      <div style={{ fontSize: 11, color: va.text2 }}>{v.contact} {v.phone && `· ${v.phone}`}</div>
                    </div>
                  </div>
                ))
              }
            </VACard>
          </div>
        </div>
      </div>

      {/* Modals */}
      {completeTarget && (
        <CompleteModal
          request={completeTarget}
          onClose={() => setCompleteTarget(null)}
          onDone={() => { setCompleteTarget(null); load() }}
        />
      )}
      {showCreate && (
        <CreateRequestModal
          dueGages={dueGages}
          vendors={vendors}
          onClose={() => setShowCreate(false)}
          onDone={() => { setShowCreate(false); load() }}
        />
      )}
    </div>
  )
}
