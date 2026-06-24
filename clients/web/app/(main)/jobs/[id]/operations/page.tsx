'use client'

import { useState, useEffect, useCallback } from 'react'
import { useParams, useSearchParams, useRouter } from 'next/navigation'
import Link from 'next/link'
import { useTranslations, useLocale } from 'next-intl'
import { api, type PartOpDto, type DimensionDto, type TechDocListDto } from '@/lib/api-client'
import { VATopbar, VABadge, VASeg, VACard, VABtn } from '@/components/va'
import { va, type VaBadgeKind } from '@/lib/va-tokens'
import { ImportDimensionsDialog } from '@/components/parts/import-dimensions-dialog'

const OP_TYPE_COLOR: Record<string, string> = {
  CNC: va.primary, TURN: va.primary, MILL: va.accent,
  GRIND: va.primaryLt, INSP: '#5D4037', WIRE: '#00695C',
}
function opColor(code?: string | null) {
  if (!code) return va.text2
  for (const [k, v] of Object.entries(OP_TYPE_COLOR)) {
    if (code.toUpperCase().includes(k)) return v
  }
  return va.text2
}

const STATUS_META: Record<string, { label: string; kind: VaBadgeKind }> = {
  Pending:  { label: 'Pending',  kind: 'warn' },
  Approved: { label: 'Approved', kind: 'ok'   },
  Rejected: { label: 'Rejected', kind: 'err'  },
}

export default function JobCustomOperationsPage() {
  const t = useTranslations('jobs')
  const locale = useLocale()
  const router = useRouter()
  const { id } = useParams<{ id: string }>()
  const jobId = Number(id)
  const searchParams = useSearchParams()
  const opIdParam = searchParams.get('opId')

  const [jobNumber, setJobNumber]     = useState('')
  const [ops, setOps]                 = useState<PartOpDto[]>([])
  const [selectedOp, setSelectedOp]   = useState<PartOpDto | null>(null)
  const [loading, setLoading]         = useState(true)

  const [tab, setTab] = useState<'documents' | 'dimension'>('documents')
  const [docs, setDocs] = useState<TechDocListDto[]>([])
  const [dims, setDims] = useState<DimensionDto[]>([])
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [showImportDims, setShowImportDims] = useState(false)

  // Load job + custom (ForJobOnly) operations
  useEffect(() => {
    let cancelled = false
    api.jobs.get(jobId).then(res => {
      if (cancelled || !res.success || !res.data) { setLoading(false); return }
      setJobNumber(res.data.jobNumber)
      const customOps = res.data.operations.filter(o => o.forJobOnly)
      setOps(customOps)
      const op = (opIdParam ? customOps.find(o => o.id === Number(opIdParam)) : null) ?? customOps[0] ?? null
      setSelectedOp(op)
      setLoading(false)
    })
    return () => { cancelled = true }
  }, [jobId, opIdParam])

  const loadDetail = useCallback(() => {
    if (!selectedOp) { setDocs([]); setDims([]); return }
    setLoadingDetail(true)
    Promise.all([
      api.techDocuments.list({ partOpId: selectedOp.id }),
      api.operations.dimensionDefinitions(selectedOp.id),
    ]).then(([docsRes, dimsRes]) => {
      if (docsRes.success && docsRes.data) setDocs(docsRes.data)
      if (dimsRes.success && dimsRes.data) setDims(dimsRes.data)
      setLoadingDetail(false)
    })
  }, [selectedOp])

  useEffect(() => { loadDetail() }, [loadDetail])

  return (
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, minHeight: 0, background: va.bg }}>
      <VATopbar
        title={loading ? '…' : jobNumber}
        breadcrumb={t('operations.breadcrumb')}
        right={
          <VABtn kind="ghost" onClick={() => router.push(`/jobs?jobId=${jobId}`)}>{t('operations.back')}</VABtn>
        }
      />

      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* ── LEFT: custom OP list ──────────────────────────────── */}
        <div className="va-scroll" style={{ width: 280, borderRight: `1px solid ${va.border}`, overflow: 'auto', background: va.surface, flexShrink: 0 }}>
          <div style={{ padding: '12px 14px', borderBottom: `1px solid ${va.separator}`, position: 'sticky', top: 0, background: va.surface, zIndex: 1 }}>
            <div style={{ fontSize: 11, fontWeight: 700, color: va.text2, textTransform: 'uppercase', letterSpacing: 0.6 }}>{t('operations.sidebarTitle')}</div>
            <div style={{ fontSize: 11, color: va.text3, marginTop: 4 }}>{t('operations.jobLabel', { job: jobNumber })}</div>
          </div>

          {loading && <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('operations.loading')}</div>}
          {!loading && ops.length === 0 && <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('operations.noOps')}</div>}

          {ops.map(op => {
            const on = selectedOp?.id === op.id
            return (
              <div key={op.id} className="va-clickable" onClick={() => { setSelectedOp(op); router.replace(`/jobs/${jobId}/operations?opId=${op.id}`) }}
                style={{ padding: '12px 16px', borderBottom: `1px solid ${va.separator}`, borderLeft: on ? `3px solid ${va.accent}` : '3px solid transparent', background: on ? va.accentBg : va.surface, display: 'flex', alignItems: 'center', gap: 10 }}>
                <span style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', minWidth: 44, height: 26, borderRadius: 6, border: `2px dashed ${va.accent}`, background: va.accentBg, color: va.accent, fontFamily: va.mono, fontWeight: 600, fontSize: 11.5, flexShrink: 0 }}>{op.opNumber}</span>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ fontSize: 12.5, fontWeight: 600, color: va.text, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{op.description ?? op.opTypeName ?? t('operations.opFallback', { op: op.opNumber })}</div>
                  <div style={{ fontSize: 10.5, color: va.text3, marginTop: 2 }}>
                    {op.opTypeName ?? '—'} · {t('operations.docCount', { count: op.docCount })} · {t('operations.dimCount', { count: op.dimCount })}
                  </div>
                </div>
              </div>
            )
          })}
        </div>

        {/* ── RIGHT: OP detail ─────────────────────────────────── */}
        {!selectedOp ? (
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: va.text3, fontSize: 13 }}>
            {t('operations.selectPrompt')}
          </div>
        ) : (
          <div className="va-scroll" style={{ flex: 1, overflow: 'auto', padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
            {/* Header */}
            <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12 }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minWidth: 56, height: 40, borderRadius: 8, border: `2px dashed ${va.accent}`, background: va.accentBg, color: va.accent, fontFamily: va.mono, fontWeight: 700, fontSize: 16, flexShrink: 0 }}>{selectedOp.opNumber}</div>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <h2 style={{ margin: 0, fontSize: 16, fontWeight: 700, color: va.text }}>{selectedOp.description ?? t('operations.opFallback', { op: selectedOp.opNumber })}</h2>
                  {selectedOp.opTypeName && (
                    <span style={{ fontSize: 10.5, fontWeight: 600, color: opColor(selectedOp.opTypeName), background: va.surface2, padding: '2px 7px', borderRadius: 4, border: `1px solid ${opColor(selectedOp.opTypeName)}33` }}>{selectedOp.opTypeName}</span>
                  )}
                  <VABadge kind="primary">{t('operations.badge')}</VABadge>
                  {selectedOp.isComplete && <VABadge kind="ok">{t('operations.done')}</VABadge>}
                </div>
                <div style={{ fontSize: 12, color: va.text2, marginTop: 4, fontFamily: va.mono }}>
                  {t('operations.setupRun', { setup: selectedOp.setupTime ?? '—', prod: selectedOp.prodTime ?? '—' })}
                </div>
              </div>
            </div>

            {/* Tabs */}
            <VASeg
              value={tab}
              onChange={id => setTab(id as 'documents' | 'dimension')}
              options={[
                { id: 'documents', label: t('operations.tabs.documents') },
                { id: 'dimension', label: t('operations.tabs.dimension') },
              ]}
            />

            {tab === 'documents' && (
              <VACard
                pad={false}
                style={{ flex: 1, minHeight: 0 }}
                right={
                  <Link href={`/documents?partOpId=${selectedOp.id}&opNumber=${selectedOp.opNumber}&jobNumber=${encodeURIComponent(jobNumber)}&backHref=${encodeURIComponent(`/jobs/${jobId}/operations?opId=${selectedOp.id}`)}`}>
                    <VABtn kind="ghost" style={{ height: 28, fontSize: 11 }}>{t('operations.documents.openAll')}</VABtn>
                  </Link>
                }
              >
                <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
                  {loadingDetail ? (
                    <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('operations.loading')}</div>
                  ) : docs.length === 0 ? (
                    <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('operations.documents.empty')}</div>
                  ) : (
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                      <thead>
                        <tr style={{ background: va.surface2 }}>
                          {(['type', 'description', 'status', 'createdBy', 'createdAt'] as const).map(key => (
                            <th key={key} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: 'left', padding: '9px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{t(`operations.documents.headers.${key}`)}</th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {docs.map(d => {
                          const sm = STATUS_META[d.status] ?? { label: d.status, kind: 'neutral' as VaBadgeKind }
                          return (
                            <tr key={d.id} className="va-row">
                              <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontWeight: 600 }}>{d.fileTypeCode}</td>
                              <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{d.description ?? '—'}</td>
                              <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}` }}>
                                <VABadge kind={sm.kind} dot={d.status === 'Pending'}>{sm.label}</VABadge>
                              </td>
                              <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{d.createdByName}</td>
                              <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2, fontFamily: va.mono }}>
                                {new Date(d.createdAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')}
                              </td>
                            </tr>
                          )
                        })}
                      </tbody>
                    </table>
                  )}
                </div>
              </VACard>
            )}

            {tab === 'dimension' && (
              <VACard
                pad={false}
                style={{ flex: 1, minHeight: 0 }}
                right={<VABtn kind="ghost" style={{ height: 28, fontSize: 11 }} onClick={() => setShowImportDims(true)}>{t('operations.dimensions.importExcel')}</VABtn>}
              >
                <div className="va-scroll" style={{ overflow: 'auto', height: '100%' }}>
                  {loadingDetail ? (
                    <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('operations.loading')}</div>
                  ) : dims.length === 0 ? (
                    <div style={{ padding: 16, fontSize: 12, color: va.text3 }}>{t('operations.dimensions.empty')}</div>
                  ) : (
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12.5 }}>
                      <thead>
                        <tr style={{ background: va.surface2 }}>
                          {(['balloon', 'category', 'nominal', 'tolPlus', 'tolMinus', 'max', 'min', 'unit', 'final'] as const).map((key, i) => (
                            <th key={key} style={{ position: 'sticky', top: 0, background: va.surface2, textAlign: i < 2 ? 'left' : 'center', padding: '9px 14px', fontSize: 9.5, color: va.text2, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6, borderBottom: `1px solid ${va.border}`, zIndex: 1 }}>{t(`operations.dimensions.headers.${key}`)}</th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {dims.map(d => (
                          <tr key={d.id} className="va-row">
                            <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontWeight: 700, color: d.isCritical ? va.err : va.text }}>
                              {d.balloonNumber}
                              {d.code && d.code !== d.balloonNumber && <span style={{ marginLeft: 4, fontWeight: 400, fontSize: 11, color: va.text3 }}>({d.code})</span>}
                            </td>
                            <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2 }}>{d.gageTypeCode ?? d.categoryCode ?? '—'}</td>
                            {d.isTextType ? (
                              <td colSpan={5} style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.nominalText ?? '—'}</td>
                            ) : (
                              <>
                                <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.nominalValue ?? '—'}</td>
                                <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.tolerancePlus ?? '—'}</td>
                                <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.toleranceMinus ?? '—'}</td>
                                <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.maxValue ?? '—'}</td>
                                <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, fontFamily: va.mono, color: va.text2, textAlign: 'center' }}>{d.minValue ?? '—'}</td>
                              </>
                            )}
                            <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, color: va.text2, textAlign: 'center' }}>{d.unit}</td>
                            <td style={{ padding: '10px 14px', borderBottom: `1px solid ${va.separator}`, textAlign: 'center' }}>{d.isFinal ? '✓' : ''}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </VACard>
            )}
          </div>
        )}
      </div>

      <ImportDimensionsDialog
        open={showImportDims} partOpId={selectedOp?.id ?? 0} opNumber={selectedOp?.opNumber ?? ''}
        onClose={() => setShowImportDims(false)}
        onImported={() => { setShowImportDims(false); loadDetail() }}
      />
    </div>
  )
}
