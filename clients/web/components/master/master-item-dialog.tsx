'use client'

import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { api, type MachineDto, type MachineGroupDto, type OpTypeDto, type DimensionCategoryDto, type FileTypeDto, type QcInlineRateDto, type JobDto, type PartOpDto } from '@/lib/api-client'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export type MasterKind = 'machine' | 'machineGroup' | 'opType' | 'dimCategory' | 'fileType' | 'qcInlineRate'
export type MasterItem = MachineDto | MachineGroupDto | OpTypeDto | DimensionCategoryDto | FileTypeDto | QcInlineRateDto

const TITLES: Record<MasterKind, string> = {
  machine: 'Máy',
  machineGroup: 'Nhóm máy',
  opType: 'Loại OP',
  dimCategory: 'Dimension Category',
  fileType: 'Loại tài liệu',
  qcInlineRate: 'Mức kiểm QC Inline',
}

const schema = z.object({
  code: z.string().max(20).optional(),
  name: z.string().max(100).optional(),
  description: z.string().optional(),
  machineType: z.string().optional(),
  serialNumber: z.string().optional(),
  folder: z.string().optional(),
  sortOrder: z.string().optional(),
  isCnc: z.boolean().optional(),
  isActive: z.boolean().optional(),
  isSegment: z.boolean().optional(),
  isGcode: z.boolean().optional(),
  isPartNumber: z.boolean().optional(),
  isRevision: z.boolean().optional(),
  isOpNumber: z.boolean().optional(),
  isJobNumber: z.boolean().optional(),
})
type FormData = z.infer<typeof schema>

type Props = { open: boolean; kind: MasterKind; item: MasterItem | null; onClose: () => void; onSaved: () => void }

export function MasterItemDialog({ open, kind, item, onClose, onSaved }: Props) {
  const [error, setError] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({ resolver: zodResolver(schema) })

  const [qrJobSearch, setQrJobSearch] = useState('')
  const [qrJobOptions, setQrJobOptions] = useState<JobDto[]>([])
  const [qrJobId, setQrJobId] = useState<number | null>(null)
  const [qrOpOptions, setQrOpOptions] = useState<PartOpDto[]>([])
  const [qrOpId, setQrOpId] = useState<number | null>(null)
  const [qrRatePercent, setQrRatePercent] = useState('10')

  useEffect(() => {
    if (kind !== 'qcInlineRate' || !open) return
    const t = setTimeout(() => {
      api.jobs.list(1, qrJobSearch || undefined).then(res => { if (res.success && res.data) setQrJobOptions(res.data) })
    }, 250)
    return () => clearTimeout(t)
  }, [kind, open, qrJobSearch])

  useEffect(() => {
    if (kind !== 'qcInlineRate' || !qrJobId) { setQrOpOptions([]); return }
    api.jobs.operations(qrJobId).then(res => { if (res.success && res.data) setQrOpOptions(res.data) })
  }, [kind, qrJobId])

  useEffect(() => {
    if (!open) return
    setError(null)
    if (!item) {
      reset({
        code: '', name: '', description: '', machineType: '', serialNumber: '', folder: '', sortOrder: '0',
        isCnc: false, isActive: true, isSegment: false, isGcode: false, isPartNumber: false, isRevision: false, isOpNumber: false, isJobNumber: false,
      })
      setQrJobId(null); setQrOpId(null); setQrRatePercent('10'); setQrJobSearch(''); setQrJobOptions([]); setQrOpOptions([])
      return
    }
    switch (kind) {
      case 'machine': {
        const m = item as MachineDto
        reset({ code: m.code, name: m.name, machineType: m.machineType ?? '', serialNumber: m.serialNumber ?? '', isCnc: m.isCnc, isActive: m.isActive })
        break
      }
      case 'machineGroup': {
        const g = item as MachineGroupDto
        reset({ code: g.code, name: g.name, isActive: g.isActive })
        break
      }
      case 'opType': {
        const o = item as OpTypeDto
        reset({ code: o.code, name: o.name ?? '', description: o.description ?? '', isActive: o.isActive })
        break
      }
      case 'dimCategory': {
        const d = item as DimensionCategoryDto
        reset({ code: d.code, name: d.name, description: d.description ?? '', isActive: d.isActive })
        break
      }
      case 'fileType': {
        const f = item as FileTypeDto
        reset({
          code: f.code, name: f.name, folder: f.folder ?? '', sortOrder: String(f.sortOrder),
          isSegment: f.isSegment, isGcode: f.isGcode, isPartNumber: f.isPartNumber, isRevision: f.isRevision,
          isOpNumber: f.isOpNumber, isJobNumber: f.isJobNumber, isActive: f.isActive,
        })
        break
      }
      case 'qcInlineRate': {
        const r = item as QcInlineRateDto
        reset({ isActive: r.isActive })
        setQrJobId(r.jobId)
        setQrOpId(r.partOpId)
        setQrRatePercent(String(r.ratePercent))
        setQrJobSearch(r.jobNumber ?? '')
        break
      }
    }
  }, [open, item, kind, reset])

  if (!open) return null

  async function onSubmit(data: FormData) {
    setError(null)
    const code = data.code?.trim() ?? ''
    const isActive = data.isActive ?? true

    if (kind !== 'qcInlineRate' && !code) { setError('Nhập mã (Code)'); return }

    switch (kind) {
      case 'machine': {
        if (!data.name?.trim()) { setError('Nhập tên máy'); return }
        const body = { code, name: data.name.trim(), machineType: data.machineType?.trim() || null, isCnc: data.isCnc ?? false, isActive, serialNumber: data.serialNumber?.trim() || null }
        const res = item
          ? await api.machines.update((item as MachineDto).id, { id: (item as MachineDto).id, ...body })
          : await api.machines.create(body)
        if (res.success) { onClose(); onSaved() } else setError(res.error ?? 'Lỗi lưu máy')
        break
      }
      case 'machineGroup': {
        if (!data.name?.trim()) { setError('Nhập tên nhóm máy'); return }
        const body = { code, name: data.name.trim(), isActive }
        const res = item
          ? await api.machineGroups.update((item as MachineGroupDto).id, { id: (item as MachineGroupDto).id, ...body })
          : await api.machineGroups.create(body)
        if (res.success) { onClose(); onSaved() } else setError(res.error ?? 'Lỗi lưu nhóm máy')
        break
      }
      case 'opType': {
        const body = { code, name: data.name?.trim() || null, description: data.description?.trim() || null, isActive }
        const res = item
          ? await api.opTypes.update((item as OpTypeDto).id, { id: (item as OpTypeDto).id, ...body })
          : await api.opTypes.create(body)
        if (res.success) { onClose(); onSaved() } else setError(res.error ?? 'Lỗi lưu loại OP')
        break
      }
      case 'dimCategory': {
        if (!data.name?.trim()) { setError('Nhập tên'); return }
        const body = { code, name: data.name.trim(), description: data.description?.trim() || null, isActive }
        const res = item
          ? await api.dimCategories.update((item as DimensionCategoryDto).id, { id: (item as DimensionCategoryDto).id, ...body })
          : await api.dimCategories.create(body)
        if (res.success) { onClose(); onSaved() } else setError(res.error ?? 'Lỗi lưu dimension category')
        break
      }
      case 'fileType': {
        if (!data.name?.trim()) { setError('Nhập tên loại tài liệu'); return }
        const body = {
          code, name: data.name.trim(), folder: data.folder?.trim() || null,
          isSegment: data.isSegment ?? false, isGcode: data.isGcode ?? false,
          isPartNumber: data.isPartNumber ?? false, isRevision: data.isRevision ?? false,
          isOpNumber: data.isOpNumber ?? false, isJobNumber: data.isJobNumber ?? false,
          sortOrder: Number(data.sortOrder) || 0, isActive,
        }
        const res = item
          ? await api.fileTypes2.update((item as FileTypeDto).id, { id: (item as FileTypeDto).id, ...body })
          : await api.fileTypes2.create(body)
        if (res.success) { onClose(); onSaved() } else setError(res.error ?? 'Lỗi lưu loại tài liệu')
        break
      }
      case 'qcInlineRate': {
        const ratePercent = Number(qrRatePercent)
        if (!Number.isFinite(ratePercent) || ratePercent < 0 || ratePercent > 100) { setError('Mức kiểm phải từ 0 đến 100'); return }
        const r = item as QcInlineRateDto | null
        const res = r
          ? await api.qcInlineRates.update(r.id, { id: r.id, ratePercent, isActive })
          : await api.qcInlineRates.create({ jobId: qrJobId, partOpId: qrOpId, ratePercent })
        if (res.success) { onClose(); onSaved() } else setError(res.error ?? 'Lỗi lưu mức kiểm QC Inline')
        break
      }
    }
  }

  const nameRequired = kind !== 'opType'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <CardHeader><CardTitle>{item ? `Sửa — ${TITLES[kind]}` : `Thêm — ${TITLES[kind]}`}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {kind !== 'qcInlineRate' && (
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Mã (Code) *</Label>
                <Input {...register('code')} placeholder="VD: M01" />
                {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>{nameRequired ? 'Tên *' : 'Tên'}</Label>
                <Input {...register('name')} />
              </div>
            </div>
            )}

            {kind === 'machine' && (
              <>
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <Label>Loại / Nhóm máy</Label>
                    <Input {...register('machineType')} placeholder="VD: CNC-LATHE" />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Số serial</Label>
                    <Input {...register('serialNumber')} />
                  </div>
                </div>
                <label className="flex items-center gap-2 text-sm">
                  <input type="checkbox" {...register('isCnc')} className="h-4 w-4" />
                  Máy CNC
                </label>
              </>
            )}

            {(kind === 'opType' || kind === 'dimCategory') && (
              <div className="space-y-1.5">
                <Label>Mô tả</Label>
                <Input {...register('description')} />
              </div>
            )}

            {kind === 'fileType' && (
              <>
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <Label>Folder (MinIO)</Label>
                    <Input {...register('folder')} placeholder="VD: drawings" />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Thứ tự hiển thị</Label>
                    <Input {...register('sortOrder')} type="number" />
                  </div>
                </div>
                <div className="grid grid-cols-3 gap-2 text-sm">
                  <label className="flex items-center gap-2"><input type="checkbox" {...register('isPartNumber')} className="h-4 w-4" /> Part Number</label>
                  <label className="flex items-center gap-2"><input type="checkbox" {...register('isRevision')} className="h-4 w-4" /> Revision</label>
                  <label className="flex items-center gap-2"><input type="checkbox" {...register('isOpNumber')} className="h-4 w-4" /> OP Number</label>
                  <label className="flex items-center gap-2"><input type="checkbox" {...register('isJobNumber')} className="h-4 w-4" /> Job Number</label>
                  <label className="flex items-center gap-2"><input type="checkbox" {...register('isGcode')} className="h-4 w-4" /> G-code</label>
                  <label className="flex items-center gap-2"><input type="checkbox" {...register('isSegment')} className="h-4 w-4" /> Segment</label>
                </div>
              </>
            )}

            {kind === 'qcInlineRate' && (
              <>
                <div className="space-y-1.5">
                  <Label>Job (để trống = áp dụng mọi Job)</Label>
                  <Input value={qrJobSearch} onChange={e => { setQrJobSearch(e.target.value); setQrJobId(null) }} placeholder="Tìm theo số Job..." disabled={!!item} />
                  {!item && qrJobOptions.length > 0 && (
                    <div className="border rounded-md max-h-32 overflow-y-auto">
                      {qrJobOptions.map(j => (
                        <div key={j.id} className={`px-2 py-1 text-sm cursor-pointer hover:bg-accent ${qrJobId === j.id ? 'bg-accent' : ''}`}
                          onClick={() => { setQrJobId(j.id); setQrJobSearch(j.jobNumber) }}>
                          {j.jobNumber}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
                <div className="space-y-1.5">
                  <Label>OP (để trống = áp dụng mọi OP của Job trên)</Label>
                  <select className="w-full h-9 rounded-md border px-2 text-sm" disabled={!!item || !qrJobId}
                    value={qrOpId ?? ''} onChange={e => setQrOpId(e.target.value ? Number(e.target.value) : null)}>
                    <option value="">— Tất cả OP —</option>
                    {qrOpOptions.map(o => <option key={o.id} value={o.id}>{o.opNumber}</option>)}
                  </select>
                </div>
                <div className="space-y-1.5">
                  <Label>Mức kiểm (%) *</Label>
                  <Input type="number" min={0} max={100} value={qrRatePercent} onChange={e => setQrRatePercent(e.target.value)} />
                </div>
              </>
            )}

            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" {...register('isActive')} className="h-4 w-4"
                disabled={kind === 'qcInlineRate' && !!item && (item as QcInlineRateDto).jobId == null && (item as QcInlineRateDto).partOpId == null} />
              Đang hoạt động (hiện trong dropdown)
            </label>

            {error && <p className="text-sm text-destructive">{error}</p>}
            <div className="flex gap-2 pt-2">
              <Button type="submit" disabled={isSubmitting} className="flex-1">
                {isSubmitting ? 'Đang lưu...' : item ? 'Lưu thay đổi' : 'Tạo mới'}
              </Button>
              <Button type="button" variant="outline" onClick={() => { reset(); onClose() }}>Huỷ</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
