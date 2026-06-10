'use client'

import { useState, useEffect } from 'react'
import { api } from '@/lib/api-client'
import { VATopbar, VACard, VABtn, VABadge } from '@/components/va'
import { va } from '@/lib/va-tokens'

const TABS = ['Máy móc', 'Nhóm máy', 'Loại OP', 'Dimension Category', 'Loại tài liệu']

type Machine = { id: number; code: string; name: string; machineType: string | null; isCnc: boolean }
type MachineGroup = { id: number; code: string; name: string; machineCount: number }
type OpType = { id: number; code: string; name: string | null }
type DimCat = { id: number; code: string; name: string; description: string | null }
type FileType = { id: number; code: string; name: string; folder: string; isPartNumber: boolean; isOpNumber: boolean; isJobNumber: boolean }

const CodeTag = ({ c }: { c: string }) => (
  <span style={{ fontFamily: va.mono, fontSize: 11, fontWeight: 700, color: va.primary, background: va.surface2, padding: '2px 7px', borderRadius: 4, border: `1px solid ${va.border}` }}>{c}</span>
)
const tdStyle: React.CSSProperties = { padding: '11px 14px', borderBottom: `1px solid ${va.separator}` }
const thStyle = (stickyBg = va.surface2): React.CSSProperties => ({ position: 'sticky', top: 0, background: stickyBg, textAlign: 'left', padding: '10px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 })

export default function MasterPage() {
  const [tab, setTab] = useState(0)
  const [machines,   setMachines]   = useState<Machine[]>([])
  const [groups,     setGroups]     = useState<MachineGroup[]>([])
  const [opTypes,    setOpTypes]    = useState<OpType[]>([])
  const [dimCats,    setDimCats]    = useState<DimCat[]>([])
  const [fileTypes,  setFileTypes]  = useState<FileType[]>([])
  const [loading,    setLoading]    = useState(true)

  useEffect(() => {
    Promise.all([
      api.machines.list(false),
      api.opTypes.list(),
      api.dimCategories.list(),
      api.fileTypes2.list(),
      api.machineGroups.list(),
    ]).then(([mRes, otRes, dcRes, ftRes, mgRes]) => {
      if (mRes.success  && mRes.data)  setMachines(mRes.data as Machine[])
      if (otRes.success && otRes.data) setOpTypes(otRes.data as OpType[])
      if (dcRes.success && dcRes.data) setDimCats(dcRes.data as DimCat[])
      if (ftRes.success && ftRes.data) setFileTypes(ftRes.data as FileType[])
      if (mgRes.success && mgRes.data) setGroups(mgRes.data as MachineGroup[])
      setLoading(false)
    })
  }, [])

  const tables = [
    // Machines
    <table key="machines" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Mã máy', 'Tên máy', 'Loại/Nhóm', 'CNC'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={4} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : machines.map(m => (
          <tr key={m.code} className="va-row va-clickable">
            <td style={tdStyle}><CodeTag c={m.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{m.name}</td>
            <td style={{ ...tdStyle, color: va.text2 }}>{m.machineType ?? '—'}</td>
            <td style={tdStyle}>{m.isCnc ? <VABadge kind="ok">CNC</VABadge> : <span style={{ color: va.text3 }}>—</span>}</td>
          </tr>
        ))}
      </tbody>
    </table>,

    // Machine Groups
    <table key="groups" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên nhóm', 'Số máy'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={3} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : groups.length === 0 ? <tr><td colSpan={3} style={tdStyle}><span style={{ color: va.text3 }}>Chưa có nhóm máy. Cần seed từ legacy data.</span></td></tr>
          : groups.map(g => (
          <tr key={g.code} className="va-row va-clickable">
            <td style={tdStyle}><CodeTag c={g.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{g.name}</td>
            <td style={{ ...tdStyle, fontFamily: va.mono, color: va.text2 }}>{g.machineCount}</td>
          </tr>
        ))}
      </tbody>
    </table>,

    // OP Types
    <table key="optypes" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên công đoạn'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={2} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : opTypes.map(o => (
          <tr key={o.code} className="va-row va-clickable">
            <td style={tdStyle}><CodeTag c={o.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{o.name ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>,

    // Dimension Categories
    <table key="dimcats" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên phương pháp đo', 'Mô tả'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={3} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : dimCats.map(d => (
          <tr key={d.code} className="va-row va-clickable">
            <td style={tdStyle}><CodeTag c={d.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{d.name}</td>
            <td style={{ ...tdStyle, color: va.text2 }}>{d.description ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>,

    // File Types
    <table key="filetypes" style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
      <thead><tr style={{ background: va.surface2 }}>
        {['Code', 'Tên', 'Folder', 'Part', 'OP', 'Job'].map((h, i) => <th key={i} style={thStyle()}>{h}</th>)}
      </tr></thead>
      <tbody>
        {loading ? <tr><td colSpan={6} style={tdStyle}><span style={{ color: va.text3 }}>Đang tải…</span></td></tr>
          : fileTypes.map(f => (
          <tr key={f.code} className="va-row va-clickable">
            <td style={tdStyle}><CodeTag c={f.code} /></td>
            <td style={{ ...tdStyle, fontWeight: 500 }}>{f.name}</td>
            <td style={{ ...tdStyle, fontFamily: va.mono, color: va.text2, fontSize: 11 }}>{f.folder}</td>
            <td style={tdStyle}>{f.isPartNumber ? '✓' : ''}</td>
            <td style={tdStyle}>{f.isOpNumber  ? '✓' : ''}</td>
            <td style={tdStyle}>{f.isJobNumber ? '✓' : ''}</td>
          </tr>
        ))}
      </tbody>
    </table>,
  ]

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar title="Master Data" breadcrumb="Hệ thống › Dữ liệu danh mục nền tảng"
        right={<VABtn kind="primary">+ Thêm mục</VABtn>} />

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
          Dữ liệu danh mục đang được tham chiếu không thể xóa — dùng <b style={{ color: va.text }}>is_active = false</b> để ẩn khỏi dropdown.
        </div>

        <VACard pad={false} style={{ flex: 1, minHeight: 0 }}>
          <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
            {tables[tab]}
          </div>
        </VACard>
      </div>
    </div>
  )
}
