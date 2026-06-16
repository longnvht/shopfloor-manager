'use client'

import { useEffect, useState } from 'react'
import { useTranslations } from 'next-intl'
import { api, type ErpConnectionDto, type ErpPreviewDto, type GlobalImportResultDto } from '@/lib/api-client'
import { va } from '@/lib/va-tokens'
import { VABtn, VABadge } from '@/components/va'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Props = { open: boolean; onClose: () => void; onImported: () => void }

type Step = 'filter' | 'preview' | 'result'

export function ErpImportDialog({ open, onClose, onImported }: Props) {
  const t = useTranslations('erp')
  const [step, setStep] = useState<Step>('filter')
  const [connections, setConnections] = useState<ErpConnectionDto[]>([])
  const [connId, setConnId] = useState<number | null>(null)
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [poNumber, setPoNumber] = useState('')
  const [testing, setTesting] = useState(false)
  const [testStatus, setTestStatus] = useState<'ok' | 'err' | null>(null)
  const [loading, setLoading] = useState(false)
  const [preview, setPreview] = useState<ErpPreviewDto | null>(null)
  const [result, setResult] = useState<GlobalImportResultDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    api.erp.connections().then(r => {
      if (r.success && r.data) {
        setConnections(r.data)
        if (r.data.length === 1) setConnId(r.data[0].id)
      }
    })
  }, [open])

  if (!open) return null

  function resetToFilter() {
    setStep('filter'); setPreview(null); setResult(null); setError(null); setTestStatus(null)
  }

  function close() {
    resetToFilter(); setConnId(null); setDateFrom(''); setDateTo(''); setPoNumber('')
    onClose()
  }

  async function handleTest() {
    if (!connId) return
    setTesting(true); setTestStatus(null)
    const r = await api.erp.testConnection(connId)
    setTesting(false)
    setTestStatus(r.success && r.data ? 'ok' : 'err')
  }

  async function handlePreview() {
    if (!connId) { setError(t('errorNoConnection')); return }
    setError(null); setLoading(true)
    const r = await api.erp.preview({
      connectionId: connId,
      dateFrom: dateFrom || undefined,
      dateTo: dateTo || undefined,
      poNumber: poNumber || undefined,
    })
    setLoading(false)
    if (r.success && r.data) {
      setPreview(r.data)
      setStep('preview')
    } else {
      setError(r.error ?? t('errorPreview'))
    }
  }

  async function handleImport() {
    if (!connId) return
    setError(null); setLoading(true)
    const r = await api.erp.import({
      connectionId: connId,
      dateFrom: dateFrom || undefined,
      dateTo: dateTo || undefined,
      poNumber: poNumber || undefined,
    })
    setLoading(false)
    if (r.success && r.data) {
      setResult(r.data)
      setStep('result')
      onImported()
    } else {
      setError(r.error ?? t('errorImport'))
    }
  }

  const selectedConn = connections.find(c => c.id === connId)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-3xl max-h-[90vh] flex flex-col">
        <CardHeader>
          <CardTitle style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <span>{t('title')}</span>
            {/* Stepper */}
            <div style={{ display: 'flex', gap: 6, marginLeft: 'auto', fontSize: 12 }}>
              {(['filter', 'preview', 'result'] as Step[]).map((s, i) => (
                <span key={s} style={{
                  padding: '2px 10px', borderRadius: 12,
                  background: step === s ? va.primary : va.surface2,
                  color: step === s ? '#fff' : va.text2,
                  fontWeight: step === s ? 600 : 400,
                }}>
                  {i + 1}. {t(`step.${s}`)}
                </span>
              ))}
            </div>
          </CardTitle>
        </CardHeader>

        <CardContent className="flex-1 overflow-y-auto space-y-4">

          {/* ── Bước 1: Bộ lọc ── */}
          {step === 'filter' && (
            <>
              <p style={{ fontSize: 13, color: va.text2 }}>{t('filterHint')}</p>

              {/* Kết nối */}
              <div>
                <label style={{ fontSize: 12, color: va.text2, display: 'block', marginBottom: 4 }}>{t('connection')}</label>
                {connections.length === 0 ? (
                  <p style={{ fontSize: 13, color: va.err }}>{t('noConnections')}</p>
                ) : (
                  <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                    <select
                      value={connId ?? ''}
                      onChange={e => { setConnId(Number(e.target.value)); setTestStatus(null) }}
                      style={{
                        flex: 1, height: 38, padding: '0 10px', borderRadius: 6,
                        border: `1px solid ${va.border}`, background: va.surface2, fontSize: 13,
                      }}
                    >
                      <option value="">{t('selectConnection')}</option>
                      {connections.map(c => (
                        <option key={c.id} value={c.id}>
                          {c.name} ({c.erpType} — {c.baseUrl})
                        </option>
                      ))}
                    </select>
                    <VABtn kind="ghost" onClick={handleTest} disabled={!connId || testing}>
                      {testing ? '...' : t('testBtn')}
                    </VABtn>
                    {testStatus === 'ok' && <VABadge kind="ok">{t('testOk')}</VABadge>}
                    {testStatus === 'err' && <VABadge kind="err">{t('testErr')}</VABadge>}
                  </div>
                )}
                {selectedConn?.erpType === 'Mock' && (
                  <p style={{ fontSize: 11.5, color: va.accent, marginTop: 4 }}>⚠ {t('mockWarning')}</p>
                )}
              </div>

              {/* Bộ lọc ngày */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
                <div>
                  <label style={{ fontSize: 12, color: va.text2, display: 'block', marginBottom: 4 }}>{t('dateFrom')}</label>
                  <input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)}
                    style={{ width: '100%', height: 38, padding: '0 10px', borderRadius: 6, border: `1px solid ${va.border}`, background: va.surface2, fontSize: 13 }} />
                </div>
                <div>
                  <label style={{ fontSize: 12, color: va.text2, display: 'block', marginBottom: 4 }}>{t('dateTo')}</label>
                  <input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)}
                    style={{ width: '100%', height: 38, padding: '0 10px', borderRadius: 6, border: `1px solid ${va.border}`, background: va.surface2, fontSize: 13 }} />
                </div>
                <div>
                  <label style={{ fontSize: 12, color: va.text2, display: 'block', marginBottom: 4 }}>{t('poNumber')}</label>
                  <input type="text" value={poNumber} onChange={e => setPoNumber(e.target.value)}
                    placeholder={t('poNumberPlaceholder')}
                    style={{ width: '100%', height: 38, padding: '0 10px', borderRadius: 6, border: `1px solid ${va.border}`, background: va.surface2, fontSize: 13 }} />
                </div>
              </div>

              {error && <p style={{ fontSize: 13, color: va.err }}>{error}</p>}
            </>
          )}

          {/* ── Bước 2: Preview ── */}
          {step === 'preview' && preview && (
            <>
              {/* Thống kê nhanh */}
              <div style={{ display: 'flex', gap: 12 }}>
                {[
                  { label: t('previewStats.jobs'), value: new Set(preview.rows.map(r => r.jobNumber)).size },
                  { label: t('previewStats.parts'), value: new Set(preview.rows.map(r => r.partNumber)).size },
                  { label: t('previewStats.ops'), value: preview.rows.length },
                ].map(s => (
                  <div key={s.label} style={{
                    flex: 1, background: va.surface2, borderRadius: 8, padding: '10px 14px', textAlign: 'center'
                  }}>
                    <div style={{ fontFamily: va.mono, fontSize: 22, fontWeight: 700, color: va.primary }}>{s.value}</div>
                    <div style={{ fontSize: 11.5, color: va.text2, marginTop: 2 }}>{s.label}</div>
                  </div>
                ))}
              </div>

              {/* Cảnh báo */}
              {preview.warnings.length > 0 && (
                <div style={{
                  background: '#FFF3E0', border: `1px solid ${va.accent}`,
                  borderRadius: 8, padding: '10px 14px',
                }}>
                  <div style={{ fontSize: 12, fontWeight: 600, color: va.accent, marginBottom: 6 }}>⚠ {t('warnings')}</div>
                  {preview.warnings.map((w, i) => (
                    <div key={i} style={{ fontSize: 12, color: va.text2 }}>• {w}</div>
                  ))}
                </div>
              )}

              {/* Bảng preview */}
              <div className="va-scroll" style={{ overflow: 'auto', maxHeight: 320, border: `1px solid ${va.border}`, borderRadius: 8 }}>
                <table style={{ width: '100%', fontSize: 12, borderCollapse: 'collapse' }}>
                  <thead>
                    {(['job', 'part', 'rev', 'qty', 'shipBy', 'op', 'opType', 'setup', 'prod'] as const).map(h => (
                      <th key={h} style={{ position: 'sticky', top: 0, background: va.surface2, padding: '7px 10px', textAlign: 'left', borderBottom: `1px solid ${va.border}`, whiteSpace: 'nowrap' }}>
                        {t(`previewTable.${h}`)}
                      </th>
                    ))}
                  </thead>
                  <tbody>
                    {preview.rows.map((r, i) => (
                      <tr key={i} style={{ borderBottom: `1px solid ${va.separator}` }}>
                        <td style={{ padding: '6px 10px', fontFamily: va.mono, fontSize: 11.5 }}>{r.jobNumber}</td>
                        <td style={{ padding: '6px 10px', fontFamily: va.mono, fontSize: 11.5 }}>{r.partNumber}</td>
                        <td style={{ padding: '6px 10px', color: va.text2 }}>{r.revision ?? '—'}</td>
                        <td style={{ padding: '6px 10px', textAlign: 'right', fontFamily: va.mono }}>{r.runQty ?? '—'}</td>
                        <td style={{ padding: '6px 10px', color: va.text2, fontSize: 11.5 }}>{r.shipBy ?? '—'}</td>
                        <td style={{ padding: '6px 10px', fontFamily: va.mono }}>{r.opNumber}</td>
                        <td style={{ padding: '6px 10px' }}>
                          {r.opTypeCode
                            ? <VABadge kind="neutral">{r.opTypeCode}</VABadge>
                            : <span style={{ color: va.text3 }}>—</span>}
                        </td>
                        <td style={{ padding: '6px 10px', textAlign: 'right', color: va.text2 }}>{r.setupTime ?? '—'}</td>
                        <td style={{ padding: '6px 10px', textAlign: 'right', color: va.text2 }}>{r.prodTime ?? '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {error && <p style={{ fontSize: 13, color: va.err }}>{error}</p>}
            </>
          )}

          {/* ── Bước 3: Kết quả ── */}
          {step === 'result' && result && (
            <>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 10 }}>
                {([
                  ['partsCreated', result.partsCreated],
                  ['partRevsCreated', result.partRevsCreated],
                  ['opsCreated', result.opsCreated],
                  ['opsUpdated', result.opsUpdated],
                  ['jobsCreated', result.jobsCreated],
                  ['jobsUpdated', result.jobsUpdated],
                  ['productsCreated', result.productsCreated],
                ] as [string, number][]).map(([k, v]) => (
                  <div key={k} style={{
                    background: v > 0 ? va.accentBg : va.surface2,
                    borderRadius: 8, padding: '10px 12px', textAlign: 'center',
                    border: `1px solid ${v > 0 ? va.accent : va.border}`,
                  }}>
                    <div style={{ fontFamily: va.mono, fontSize: 24, fontWeight: 700, color: v > 0 ? va.accent : va.text3 }}>{v}</div>
                    <div style={{ fontSize: 11, color: va.text2, marginTop: 2 }}>{t(`result.${k}`)}</div>
                  </div>
                ))}
              </div>

              {result.errors.length > 0 && (
                <div style={{ border: `1px solid ${va.err}`, borderRadius: 8, padding: '10px 14px', background: '#FFF5F5' }}>
                  <div style={{ fontSize: 12, fontWeight: 600, color: va.err, marginBottom: 6 }}>
                    {t('result.errors', { count: result.errors.length })}
                  </div>
                  <ul style={{ maxHeight: 180, overflow: 'auto' }}>
                    {result.errors.map((e, i) => (
                      <li key={i} style={{ fontSize: 12, color: va.text2, marginBottom: 4 }}>
                        {t('result.rowError', { row: e.rowNumber, message: e.message })}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </>
          )}

        </CardContent>

        {/* Footer buttons */}
        <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', padding: '12px 24px 20px' }}>
          {step === 'filter' && (
            <>
              <VABtn kind="ghost" onClick={close}>{t('cancel')}</VABtn>
              <VABtn kind="primary" disabled={!connId || loading} onClick={handlePreview}>
                {loading ? t('loading') : t('previewBtn')}
              </VABtn>
            </>
          )}
          {step === 'preview' && (
            <>
              <VABtn kind="ghost" onClick={resetToFilter}>{t('back')}</VABtn>
              <VABtn kind="primary" disabled={loading || (preview?.rows.length ?? 0) === 0} onClick={handleImport}>
                {loading ? t('loading') : t('importBtn', { count: preview?.rows.length ?? 0 })}
              </VABtn>
            </>
          )}
          {step === 'result' && (
            <VABtn kind="primary" onClick={close}>{t('close')}</VABtn>
          )}
        </div>
      </Card>
    </div>
  )
}
