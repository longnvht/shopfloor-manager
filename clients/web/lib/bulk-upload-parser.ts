import type { FileTypeDto, ResolveBatchItem, ResolveBatchResultDto } from './api-client'

export const SEGMENT_RE = /^(\d+)_(\d+)$/

export type ParsedFile = {
  fileName: string
  ext: string
  fileTypeCode: string | null
  partNumber: string | null
  partRevCode: string | null
  routingRevCode: string | null
  opNumber: string | null
  jobNumber: string | null
  segmentIndex: number | null
  segmentTotal: number | null
}

/**
 * Parse tên file theo quy ước:
 *   Standard OP:   {PartNumber}-{PartRevCode}-{RoutingRevCode}-{OPNumber}-{FileTypeCode}[-{i}_{n}].ext
 *   Part-level:    {PartNumber}-{PartRevCode}-{FileTypeCode}.ext
 *   ForJobOnly OP: {JobNumber}-{OPNumber}-{FileTypeCode}.ext
 * Token cuối (FileTypeCode) được nhận diện qua danh sách FileTypeDto — các cờ
 * isJobNumber/isPartNumber/isOpNumber quyết định cấu trúc các token còn lại.
 * PartNumber/JobNumber có thể tự chứa "-" — phần còn lại được join lại bằng "-".
 */
export function parseFileName(fileName: string, fileTypes: FileTypeDto[]): ParsedFile {
  const dot = fileName.lastIndexOf('.')
  const ext = dot >= 0 ? fileName.slice(dot) : ''
  const base = dot >= 0 ? fileName.slice(0, dot) : fileName
  const tokens = base.split('-')

  let segmentIndex: number | null = null
  let segmentTotal: number | null = null
  const segMatch = tokens.length > 0 ? SEGMENT_RE.exec(tokens[tokens.length - 1]) : null
  if (segMatch) {
    segmentIndex = parseInt(segMatch[1], 10)
    segmentTotal = parseInt(segMatch[2], 10)
    tokens.pop()
  }

  const typeToken = tokens.pop()
  const fileType = fileTypes.find(ft => ft.code.toUpperCase() === (typeToken ?? '').toUpperCase())

  const base_result: ParsedFile = {
    fileName, ext,
    fileTypeCode: fileType?.code ?? null,
    partNumber: null, partRevCode: null, routingRevCode: null,
    opNumber: null, jobNumber: null, segmentIndex, segmentTotal,
  }

  if (!fileType) return base_result

  if (fileType.isJobNumber && fileType.isOpNumber) {
    const opNumber = tokens.pop() ?? null
    const jobNumber = tokens.length > 0 ? tokens.join('-') : null
    return { ...base_result, opNumber, jobNumber }
  }

  if (fileType.isPartNumber && fileType.isOpNumber) {
    const opNumber = tokens.pop() ?? null
    const routingRevCode = tokens.pop() ?? null
    const partRevCode = tokens.pop() ?? null
    const partNumber = tokens.length > 0 ? tokens.join('-') : null
    return { ...base_result, opNumber, routingRevCode, partRevCode, partNumber }
  }

  // Part-level (DRW/CAD): {PartNumber}-{PartRevCode}-{FileTypeCode}
  const partRevCode = tokens.pop() ?? null
  const partNumber = tokens.length > 0 ? tokens.join('-') : null
  return { ...base_result, partRevCode, partNumber }
}

export function toResolveBatchItem(p: ParsedFile, fileSizeBytes: number): ResolveBatchItem {
  return {
    fileName: p.fileName,
    fileTypeCode: p.fileTypeCode ?? '',
    partNumber: p.partNumber,
    partRevCode: p.partRevCode,
    routingRevCode: p.routingRevCode,
    opNumber: p.opNumber,
    jobNumber: p.jobNumber,
    segmentIndex: p.segmentIndex,
    segmentTotal: p.segmentTotal,
    fileSizeBytes,
  }
}

/** Token segment gốc (vd "1_3") dùng để lưu vào TechDocument.Segment khi upload. */
export function segmentToken(p: ParsedFile): string | null {
  return p.segmentIndex != null && p.segmentTotal != null ? `${p.segmentIndex}_${p.segmentTotal}` : null
}

export type BatchStatus = 'Ready' | 'Duplicate' | 'Invalid' | 'SegmentIncomplete' | 'Uploading' | 'Success' | 'Error'

export type BatchRow = {
  file: File
  fileName: string
  parsed: ParsedFile
  resolve: ResolveBatchResultDto | null
  status: BatchStatus
  reason: string | null
  /** Raw, untranslated error captured at upload time (network/MinIO/create() failure) — distinct from `reason` (resolve-time enum). */
  errorMessage?: string | null
}

export function buildBatchRows(files: File[], fileTypes: FileTypeDto[]): BatchRow[] {
  return files.map(file => ({
    file,
    fileName: file.name,
    parsed: parseFileName(file.name, fileTypes),
    resolve: null,
    status: 'Invalid',
    reason: null,
    errorMessage: null,
  }))
}

export function mergeResolveResults(rows: BatchRow[], results: ResolveBatchResultDto[]): BatchRow[] {
  return rows.map((row, i) => {
    const resolve = results[i] ?? null
    return {
      ...row,
      resolve,
      status: (resolve?.status as BatchStatus) ?? 'Invalid',
      reason: resolve?.reason ?? null,
    }
  })
}

function dedupKey(resolve: ResolveBatchResultDto, parsed: ParsedFile): string {
  return [resolve.fileTypeId, resolve.partRevId, resolve.partOpId, resolve.jobId, parsed.segmentIndex ?? ''].join(':')
}

function segmentGroupKey(resolve: ResolveBatchResultDto): string {
  return [resolve.fileTypeId, resolve.partRevId, resolve.partOpId, resolve.jobId].join(':')
}

/**
 * Áp dụng kiểm tra phía client lên các row đang "Ready":
 * 1. Trùng lặp trong cùng lô (cùng fileType+Part/OP/Job+segment) → "Duplicate" (giữ row đầu tiên).
 * 2. Segment thiếu — group theo fileType+Part/OP/Job, kiểm tra đủ 1..segmentTotal
 *    (tính cả existingSegments đã có trên server) → nếu thiếu, toàn bộ group → "SegmentIncomplete".
 */
export function applyClientChecks(rows: BatchRow[]): BatchRow[] {
  const result = rows.map(r => ({ ...r }))

  const seen = new Set<string>()
  for (const r of result) {
    if (r.status !== 'Ready' || !r.resolve) continue
    const key = dedupKey(r.resolve, r.parsed)
    if (seen.has(key)) {
      r.status = 'Duplicate'
      r.reason = 'duplicateOf'
    } else {
      seen.add(key)
    }
  }

  const groups = new Map<string, BatchRow[]>()
  for (const r of result) {
    if (r.status !== 'Ready' || !r.resolve || !r.parsed.segmentTotal) continue
    const key = segmentGroupKey(r.resolve)
    const arr = groups.get(key) ?? []
    arr.push(r)
    groups.set(key, arr)
  }

  for (const groupRows of groups.values()) {
    const total = groupRows[0].parsed.segmentTotal!
    const present = new Set<number>()
    for (const r of groupRows) if (r.parsed.segmentIndex != null) present.add(r.parsed.segmentIndex)
    for (const seg of groupRows[0].resolve!.existingSegments ?? []) {
      const m = SEGMENT_RE.exec(seg)
      if (m) present.add(parseInt(m[1], 10))
    }

    const complete = Array.from({ length: total }, (_, i) => i + 1).every(n => present.has(n))
    if (!complete) {
      for (const r of groupRows) {
        r.status = 'SegmentIncomplete'
        r.reason = 'segmentMissing'
      }
    }
  }

  return result
}

/** Mô tả ngắn Part/Rev/Routing/OP hoặc Job/OP đã nhận diện được, dùng cho cột "Nhận diện". */
export function describeMatch(r: ResolveBatchResultDto): string {
  if (r.resolvedJobNumber) {
    return `${r.resolvedJobNumber} · OP ${r.resolvedOpNumber}`
  }
  if (r.resolvedPartNumber) {
    const parts = [r.resolvedPartNumber]
    if (r.resolvedRevCode) parts.push(`Rev ${r.resolvedRevCode}`)
    if (r.resolvedRoutingRevCode) parts.push(r.resolvedRoutingRevCode)
    if (r.resolvedOpNumber) parts.push(`OP ${r.resolvedOpNumber}`)
    return parts.join(' · ')
  }
  return '—'
}
