'use client'

import { useState, useEffect, useCallback } from 'react'
import { api, type MachineDto, type MachineGroupDto, type OpTypeDto, type GageCategoryDto, type FileTypeDto, type QcInlineRateDto } from '@/lib/api-client'
import { VATopbar, VACard, VABtn, VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'
import { MasterItemDialog, type MasterKind, type MasterItem } from '@/components/master/master-item-dialog'

const TABS = ['Máy móc', 'Nhóm máy', 'Loại OP', 'Gage Category', 'Loại tài liệu', 'Mức kiểm QC Inline']
const TAB_KINDS: MasterKind[] = ['machine', 'machineGroup', 'opType', 'gageCategory', 'fileType', 'qcInlineRate']

const CodeTag = ({ c }: { c: string }) => (
  <span style={{ fontFamily: va.mono, fontSize: 11, fontWeight: 700, color: va.primary, background: va.surface2, padding: '2px 7px', borderRadius: 4, border: `1px solid ${va.border}` }}>{c}</span>
)
const ActiveBadge = ({ active }: { active: boolean }) =>
  active ? <VABadge kind="ok">Hoạt động</VABadge> : <VABadge kind="neutral">Đã ẩn</VABadge>

const tdStyle: React.CSSProperties = { padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }
const thStyle = (stickyBg = va.surface2): React.CSSProperties => ({ position: 'sticky', top: 0, background: stickyBg, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 })

export default function MasterPage() {
  const [tab, setTab] = useState(0)
  const [machines,   setMachines]   = useState<MachineDto[]>([])
  const [groups,     setGroups]     = useState<MachineGroupDto[]>([])
  const [opTypes,    setOpTypes]    = useState<OpTypeDto[]>([])
  const [gageCats,    setGageCats]    = useState<GageCategoryDto[]>([])
  const [fileTypes,  setFileTypes]  = useState<FileTypeDto[]>([])
  const [qcRates,    setQcRates]    = useState<QcInlineRateDto[]>([])
  const [loading,    setLoading]    = useState(true)

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingItem, setEditingItem] = useState<MasterItem | null>(null)

  const load = useCallback(() => {
    setLoading(true)
    Promise.all([
      api.machines.list(false),
      api.opTypes.list(),
      api.gageCategories.list(),
      api.fileTypes2.list(),
      api.machineGroups.list(),
      api.qcInlineRates.list(),
    ]).then(([mRes, otRes, dcRes, ftRes, mgRes, qrRes]) => {
      if (mRes.success  && mRes.data)  setMachines(mRes.data)
      if (otRes.success && otRes.data) setOpTypes(otRes.data)
      if (dcRes.success && dcRes.data) setGageCats(dcRes.data)
      if (ftRes.success && ftRes.data) setFileTypes(ftRes.data)
      if (mgRes.success && mgRes.data) setGroups(mgRes.data)
      if (qrRes.success && qrRes.data) setQcRates(qrRes.data)
      setLoading(false)
    })
  }, [])

  useEffect(() => { load() }, [load])

  function openCreate() {
    setEditingItem(null)
    setDialogOpen(true)
  }
  function openEdit(item: MasterItem) {
    setEditingItem(item)
    setDialogOpen(true)
  }
  function onSaved() {
    setDialogOpen(false)
    setEditingItem(null)
    load()
  }

  const tables = [
    // Machines
    <table key="machines" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Mã máy', 'Tên máy', 'Loại/Nhóm', 'CNC', 'Trạng thái'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={5} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : machines.map(m => (
          <tr key={m.id} className="va-row va-clickable" onClick={() => openEdit(m)}>
            <td style={tdStyle}><CodeTag c={m.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{m.name}</td>
            <td style={{ ...tdStyle, color: va.text2 }}>{m.machineType ?? '—'}</td>
            <td style={tdStyle}>{m.isCnc ? <VABadge kind="ok">CNC</VABadge> : <span style={{ color: va.text3 }}>—</span>}</td>
            <td style={tdStyle}><ActiveBadge active={m.isActive} /></td>
          </tr>
        ))}
      </tbody>
    </table>,

    // Machine Groups
    <table key="groups" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên nhóm', 'Số máy', 'Trạng thái'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={4} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : groups.length === 0 ? <tr><td colSpan={4} style={tdStyle}><span style={{ color: va.text3 }}>Chưa có nhóm máy.</span></td></tr>
          : groups.map(g => (
          <tr key={g.id} className="va-row va-clickable" onClick={() => openEdit(g)}>
            <td style={tdStyle}><CodeTag c={g.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{g.name}</td>
            <td style={{ ...tdStyle, fontFamily: va.mono, color: va.text2 }}>{g.machineCount}</td>
            <td style={tdStyle}><ActiveBadge active={g.isActive} /></td>
          </tr>
        ))}
      </tbody>
    </table>,

    // OP Types
    <table key="optypes" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên công đoạn', 'Trạng thái'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={3} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : opTypes.map(o => (
          <tr key={o.id} className="va-row va-clickable" onClick={() => openEdit(o)}>
            <td style={tdStyle}><CodeTag c={o.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{o.name ?? '—'}</td>
            <td style={tdStyle}><ActiveBadge active={o.isActive} /></td>
          </tr>
        ))}
      </tbody>
    </table>,

    // Gage Categories
    <table key="dimcats" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên phương pháp đo', 'Mô tả', 'Trạng thái'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={4} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : gageCats.map(d => (
          <tr key={d.id} className="va-row va-clickable" onClick={() => openEdit(d)}>
            <td style={tdStyle}><CodeTag c={d.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{d.name}</td>
            <td style={{ ...tdStyle, color: va.text2 }}>{d.description ?? '—'}</td>
            <td style={tdStyle}><ActiveBadge active={d.isActive} /></td>
          </tr>
        ))}
      </tbody>
    </table>,

    // File Types
    <table key="filetypes" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên', 'Folder', 'Part', 'OP', 'Job', 'Trạng thái'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={7} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : fileTypes.map(f => (
          <tr key={f.id} className="va-row va-clickable" onClick={() => openEdit(f)}>
            <td style={tdStyle}><CodeTag c={f.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{f.name}</td>
            <td style={{ ...tdStyle, fontFamily: va.mono, color: va.text2, fontSize: 11 }}>{f.folder}</td>
            <td style={tdStyle}>{f.isPartNumber ? '✓' : ''}</td>
            <td style={tdStyle}>{f.isOpNumber  ? '✓' : ''}</td>
            <td style={tdStyle}>{f.isJobNumber ? '✓' : ''}</td>
            <td style={tdStyle}><ActiveBadge active={f.isActive} /></td>
          </tr>
        ))}
      </tbody>
    </table>,

    // QC Inline Rates
    <table key="qcrates" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Job', 'OP', 'Mức kiểm (%)', 'Trạng thái'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={4} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : qcRates.map(r => (
          <tr key={r.id} className="va-row va-clickable" onClick={() => openEdit(r)}>
            <td style={tdStyle}>{r.jobNumber ?? <span style={{ color: va.text3 }}>— Tất cả Job —</span>}</td>
            <td style={tdStyle}>{r.opNumber ?? <span style={{ color: va.text3 }}>— Tất cả OP —</span>}</td>
            <td style={{ ...tdStyle, fontFamily: va.mono, fontWeight: 600 }}>{r.ratePercent}%</td>
            <td style={tdStyle}><ActiveBadge active={r.isActive} /></td>
          </tr>
        ))}
      </tbody>
    </table>,
  ]

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar title="Master Data" breadcrumb="Hệ thống › Dữ liệu danh mục nền tảng"
        right={<VABtn kind="primary" onClick={openCreate}>+ Thêm mục</VABtn>} />

      <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 22, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {/* Tabs */}
        <div style={{ display: 'flex', gap: 4, borderBottom: `1px solid ${va.border}` }}>
          {TABS.map((t, i) => {
            const on = tab === i
            return (
              <div key={t} className="va-clickable" onClick={() => setTab(i)}
                style={{ padding: '10px 16px', fontSize: 13, fontWeight: on ? 600 : 500, color: on ? va.primary : va.text2, borderBottom: on ? `2px solid ${va.accent}` : '2px solid transparent', marginBottom: -1 }}>
                {t}
              </div>
            )
          })}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', background: va.accentBg, borderRadius: 9, fontSize: 12, color: va.text2 }}>
          <span style={{ color: va.accent, fontSize: 14 }}>ⓘ</span>
          Dữ liệu danh mục đang được tham chiếu không thể xóa — dùng <b style={{ color: va.text }}>is_active = false</b> để ẩn khỏi dropdown. Bấm vào một dòng để sửa.
        </div>

        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            {tables[tab]}
          </div>
        </VACard>
      </div>

      <MasterItemDialog
        open={dialogOpen}
        kind={TAB_KINDS[tab]}
        item={editingItem}
        onClose={() => { setDialogOpen(false); setEditingItem(null) }}
        onSaved={onSaved}
      />
    </div>
  )
}
