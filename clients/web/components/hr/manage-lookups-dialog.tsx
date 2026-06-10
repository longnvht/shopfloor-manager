'use client'

import { useEffect, useState } from 'react'
import { api, type DepartmentDto, type PositionDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Props = { open: boolean; onClose: () => void }

export function ManageLookupsDialog({ open, onClose }: Props) {
  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [positions, setPositions] = useState<PositionDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const [editingDept, setEditingDept] = useState<number | null>(null)
  const [deptForm, setDeptForm] = useState({ code: '', name: '' })
  const [newDept, setNewDept] = useState({ code: '', name: '' })
  const [newPos, setNewPos] = useState({ code: '', description: '' })

  function load() {
    api.lookups.departments().then(res => { if (res.success && res.data) setDepartments(res.data) })
    api.lookups.positions().then(res => { if (res.success && res.data) setPositions(res.data) })
  }

  useEffect(() => { if (open) { load(); setError(null) } }, [open])

  if (!open) return null

  async function addDepartment() {
    setError(null)
    if (!newDept.code.trim() || !newDept.name.trim()) { setError('Nhập đủ mã và tên phòng ban'); return }
    const res = await api.lookups.createDepartment({ code: newDept.code.trim(), name: newDept.name.trim() })
    if (res.success) { setNewDept({ code: '', name: '' }); load() }
    else setError(res.error ?? 'Lỗi tạo phòng ban')
  }

  function startEditDept(d: DepartmentDto) {
    setEditingDept(d.id)
    setDeptForm({ code: d.code, name: d.name })
  }

  async function saveDept(id: number) {
    setError(null)
    if (!deptForm.code.trim() || !deptForm.name.trim()) { setError('Nhập đủ mã và tên phòng ban'); return }
    const res = await api.lookups.updateDepartment(id, { code: deptForm.code.trim(), name: deptForm.name.trim() })
    if (res.success) { setEditingDept(null); load() }
    else setError(res.error ?? 'Lỗi cập nhật phòng ban')
  }

  async function addPosition() {
    setError(null)
    if (!newPos.code.trim()) { setError('Nhập mã chức vụ'); return }
    const res = await api.lookups.createPosition({ code: newPos.code.trim(), description: newPos.description.trim() || undefined })
    if (res.success) { setNewPos({ code: '', description: '' }); load() }
    else setError(res.error ?? 'Lỗi tạo chức vụ')
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <CardHeader><CardTitle>Danh mục — Phòng ban & Chức vụ</CardTitle></CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-6">
            {/* Departments */}
            <div className="space-y-3">
              <h3 className="text-sm font-semibold">Phòng ban</h3>
              <div className="space-y-1.5 max-h-64 overflow-y-auto pr-1">
                {departments.map(d => (
                  <div key={d.id} className="flex items-center gap-2 text-sm border-b pb-1.5">
                    {editingDept === d.id ? (
                      <>
                        <Input value={deptForm.code} onChange={e => setDeptForm(f => ({ ...f, code: e.target.value }))} className="w-20" />
                        <Input value={deptForm.name} onChange={e => setDeptForm(f => ({ ...f, name: e.target.value }))} className="flex-1" />
                        <Button type="button" size="sm" onClick={() => saveDept(d.id)}>Lưu</Button>
                        <Button type="button" size="sm" variant="outline" onClick={() => setEditingDept(null)}>Huỷ</Button>
                      </>
                    ) : (
                      <>
                        <span className="font-mono text-xs w-20 shrink-0">{d.code}</span>
                        <span className="flex-1">{d.name}</span>
                        <Button type="button" size="sm" variant="outline" onClick={() => startEditDept(d)}>Sửa</Button>
                      </>
                    )}
                  </div>
                ))}
                {departments.length === 0 && <p className="text-xs text-muted-foreground">Chưa có phòng ban.</p>}
              </div>
              <div className="flex items-center gap-2 pt-2">
                <Input placeholder="Mã" value={newDept.code} onChange={e => setNewDept(f => ({ ...f, code: e.target.value }))} className="w-20" />
                <Input placeholder="Tên phòng ban" value={newDept.name} onChange={e => setNewDept(f => ({ ...f, name: e.target.value }))} className="flex-1" />
                <Button type="button" size="sm" onClick={addDepartment}>+ Thêm</Button>
              </div>
            </div>

            {/* Positions */}
            <div className="space-y-3">
              <h3 className="text-sm font-semibold">Chức vụ</h3>
              <div className="space-y-1.5 max-h-64 overflow-y-auto pr-1">
                {positions.map(p => (
                  <div key={p.id} className="flex items-center gap-2 text-sm border-b pb-1.5">
                    <span className="font-mono text-xs w-24 shrink-0">{p.code}</span>
                    <span className="flex-1 text-muted-foreground">{p.description ?? '—'}</span>
                  </div>
                ))}
                {positions.length === 0 && <p className="text-xs text-muted-foreground">Chưa có chức vụ.</p>}
              </div>
              <div className="flex items-center gap-2 pt-2">
                <Input placeholder="Mã" value={newPos.code} onChange={e => setNewPos(f => ({ ...f, code: e.target.value }))} className="w-24" />
                <Input placeholder="Mô tả" value={newPos.description} onChange={e => setNewPos(f => ({ ...f, description: e.target.value }))} className="flex-1" />
                <Button type="button" size="sm" onClick={addPosition}>+ Thêm</Button>
              </div>
            </div>
          </div>

          {error && <p className="text-sm text-destructive pt-3">{error}</p>}
          <div className="flex justify-end pt-4">
            <Button type="button" variant="outline" onClick={onClose}>Đóng</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
