const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5066'

export type ApiResponse<T> = {
  success: boolean
  data: T | null
  error: string | null
  pagination: { page: number; pageSize: number; total: number; totalPages: number } | null
}

function getToken(): string | null {
  if (typeof window === 'undefined') return null
  return localStorage.getItem('auth-token')
}

async function request<T>(path: string, init?: RequestInit): Promise<ApiResponse<T>> {
  const token = getToken()
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  })
  if (!res.ok && res.status !== 400 && res.status !== 401 && res.status !== 404) {
    throw new Error(`HTTP ${res.status}`)
  }
  return res.json()
}

// FormData uploads — không set Content-Type, browser tự gắn multipart boundary
async function requestMultipart<T>(path: string, formData: FormData): Promise<ApiResponse<T>> {
  const token = getToken()
  const res = await fetch(`${API_URL}${path}`, {
    method: 'POST',
    body: formData,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  })
  if (!res.ok && res.status !== 400 && res.status !== 401 && res.status !== 404) {
    throw new Error(`HTTP ${res.status}`)
  }
  return res.json()
}

// Binary downloads (Excel templates) — trả về Blob, không parse JSON
async function requestBlob(path: string): Promise<Blob> {
  const token = getToken()
  const res = await fetch(`${API_URL}${path}`, {
    headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
  })
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  return res.blob()
}

// ── Types ─────────────────────────────────────────────────────

export type PartDto = {
  id: number; partNumber: string; description: string; createdAt: string
  currentRoutingRevCode: string | null; opCount: number; jobCount: number
}

export type PartRevDto = {
  id: number; partId: number; partNumber: string; revCode: string
  description: string | null; isActive: boolean; isReleased: boolean; createdAt: string
  createdByName: string | null
}

export type RoutingRevDto = {
  id: number; routingId: number; revCode: string; changeNote: string | null
  isActive: boolean; isReleased: boolean; createdAt: string; opCount: number
}

export type JobDto = {
  id: number; jobNumber: string
  partId: number; partRevId: number; partNumber: string; revCode: string
  routingRevId: number; routingRevCode: string
  runQty: number | null; completedCount: number; shipBy: string | null; isComplete: boolean; createdAt: string
}

export type PartOpDto = {
  id: number; routingRevId: number | null; jobId: number | null; forJobOnly: boolean
  opNumber: string; opNumberSort: number | null
  opTypeId: number | null; opTypeName: string | null
  description: string | null; note: string | null
  setupTime: number | null; prodTime: number | null; isVisible: boolean; isComplete: boolean
  dimCount: number; docCount: number
}

export type JobDetailDto = JobDto & {
  partDescription: string
  operations: PartOpDto[]
  products: ProductDto[]
}

// sessionStatus: "none" | "claimed" | "inprogress" — kết hợp với isComplete ở UI để ra 4 trạng thái hiển thị
export type ProductDto = {
  id: number; serialNumber: string; jobId: number; isComplete: boolean; sortOrder: number | null
  sessionStatus: string; claimedByName: string | null
}

export type JobProgressDto = { totalDim: number; completeDim: number; passDim: number; failDim: number }

export type DimensionDto = {
  id: number; partOpId: number
  balloonNumber: string; balloonSort: number | null; code: string | null; description: string | null
  nominalValue: number | null; tolerancePlus: number | null; toleranceMinus: number | null
  maxValue: number | null; minValue: number | null; unit: string
  isTextType: boolean; nominalText: string | null
  categoryCode: string | null; isCritical: boolean; isFinal: boolean; sortOrder: number
  status: string; reviewedBy: number | null; reviewedAt: string | null; reviewNote: string | null
}

export type RoutingRevDimensionDto = {
  id: number; opId: number; opNumber: string; opNumberSort: number | null
  balloonNumber: string; balloonSort: number | null; code: string | null; description: string | null
  nominalValue: number | null; tolerancePlus: number | null; toleranceMinus: number | null
  maxValue: number | null; minValue: number | null; unit: string
  isTextType: boolean; nominalText: string | null
  categoryCode: string | null; isCritical: boolean; isFinal: boolean; sortOrder: number
  status: string; reviewedBy: number | null; reviewedAt: string | null; reviewNote: string | null
}

export type FaiSheetDto = {
  partOpId: number; jobId: number; opNumber: string
  dimensions: DimensionDto[]
  rows: {
    serialNumber: string; productId: number; allPass: boolean
    cells: { measureValueId: number | null; balloonNumber: string; value: number | null; result: string | null }[]
  }[]
}

export type NcrDto = {
  id: number; ncrNumber: string
  jobId: number; jobNumber: string
  productId: number | null; serialNumber: string | null
  partOpId: number | null; opNumber: string | null
  description: string; status: string
  raisedBy: string; raisedAt: string
  closedBy: string | null; closedAt: string | null
}

export type NcrLogDto = {
  id: number; action: string; note: string | null
  actionBy: string; actionAt: string
}

export type NcrDetailDto = {
  ncr: NcrDto; logs: NcrLogDto[]
}

export type ImportRowError = { rowNumber: number; message: string }
export type ImportResultDto = { created: number; updated: number; skipped: number; errors: ImportRowError[] }
export type GlobalImportResultDto = {
  partsCreated: number; partRevsCreated: number
  opsCreated: number; opsUpdated: number
  jobsCreated: number; jobsUpdated: number; productsCreated: number
  errors: ImportRowError[]
}

// ERP Integration
export type ErpConnectionDto = {
  id: number; name: string; erpType: string; baseUrl: string
  company: string | null; username: string | null; hasPassword: boolean; isActive: boolean
}

export type ErpPreviewRowDto = {
  partNumber: string; partDescription: string | null; revision: string | null
  jobNumber: string; poNumber: string | null; poLine: string | null
  runQty: number | null; shipBy: string | null
  opNumber: string; opTypeCode: string | null; opDescription: string | null
  setupTime: number | null; prodTime: number | null
}

export type ErpPreviewDto = {
  rows: ErpPreviewRowDto[]
  totalCount: number
  warnings: string[]
}

export type SpcDto = {
  dimensionId: number; code: string; nominal: number
  upperLimit: number; lowerLimit: number; unit: string
  sampleCount: number; mean: number; stdDev: number
  cp: number; cpu: number; cpl: number; cpk: number; values: number[]
}

// ── API ───────────────────────────────────────────────────────

export const api = {
  auth: {
    login: (userLogin: string, password: string) =>
      request<{ token: string; userId: number; name: string; role: string; firstLogin: boolean }>(
        '/api/v1/auth/login',
        { method: 'POST', body: JSON.stringify({ userLogin, password }) }
      ),
    forgotPassword: (email: string) =>
      request<null>('/api/v1/auth/forgot-password', { method: 'POST', body: JSON.stringify({ email }) }),
    resetPassword: (email: string, code: string, newPassword: string) =>
      request<null>('/api/v1/auth/reset-password', {
        method: 'POST', body: JSON.stringify({ email, code, newPassword }),
      }),
  },
  users: {
    list: (page = 1, search?: string) => {
      const q = new URLSearchParams({ page: String(page), pageSize: '50' })
      if (search) q.set('search', search)
      return request<UserListDto[]>(`/api/v1/users?${q}`)
    },
    create: (body: { userLogin: string; password: string; name: string; email?: string; roleId?: number; userTypeId?: number; positionId?: number; workStatusId?: number }) =>
      request<UserListDto>('/api/v1/users', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: { name: string; email?: string; sex?: string; roleId?: number; userTypeId?: number; positionId?: number; workStatusId?: number; isActive: boolean }) =>
      request<UserListDto>(`/api/v1/users/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    changePassword: (currentPassword: string, newPassword: string) =>
      request<null>('/api/v1/users/me/change-password', {
        method: 'POST', body: JSON.stringify({ currentPassword, newPassword }),
      }),
  },
  jobs: {
    list: (page = 1, search?: string) =>
      request<JobDto[]>(`/api/v1/jobs?page=${page}&pageSize=20${search ? `&search=${encodeURIComponent(search)}` : ''}`),
    get: (id: number) => request<JobDetailDto>(`/api/v1/jobs/${id}`),
    create: (body: { jobNumber: string; partRevId: number; routingRevId: number; runQty?: number; shipBy?: string }) =>
      request<JobDto>('/api/v1/jobs', { method: 'POST', body: JSON.stringify(body) }),
    operations: (id: number) => request<PartOpDto[]>(`/api/v1/jobs/${id}/operations`),
    products: (id: number) => request<ProductDto[]>(`/api/v1/jobs/${id}/products`),
    progress: (id: number) => request<JobProgressDto>(`/api/v1/jobs/${id}/progress`),
    generateProducts: (id: number, quantity: number) =>
      request<ProductDto[]>(`/api/v1/jobs/${id}/products/generate`, {
        method: 'POST', body: JSON.stringify({ quantity }),
      }),
    importBatch: (file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      return requestMultipart<GlobalImportResultDto>('/api/v1/jobs/import-batch', formData)
    },
    importBatchTemplate: () => requestBlob('/api/v1/jobs/import-batch/template'),
  },
  lookups: {
    roles: () => request<{ id: number; name: string }[]>('/api/v1/roles'),
    departments: () => request<DepartmentDto[]>('/api/v1/departments'),
    createDepartment: (body: { code: string; name: string }) =>
      request<DepartmentDto>('/api/v1/departments', { method: 'POST', body: JSON.stringify(body) }),
    updateDepartment: (id: number, body: { code: string; name: string }) =>
      request<DepartmentDto>(`/api/v1/departments/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    positions: () => request<PositionDto[]>('/api/v1/positions'),
    createPosition: (body: { code: string; description?: string }) =>
      request<PositionDto>('/api/v1/positions', { method: 'POST', body: JSON.stringify(body) }),
    userTypes: () => request<UserTypeDto[]>('/api/v1/user-types'),
    workStatuses: () => request<WorkStatusDto[]>('/api/v1/work-statuses'),
  },
  fai: {
    sheet: (partOpId: number, jobId: number) =>
      request<FaiSheetDto>(`/api/v1/fai?partOpId=${partOpId}&jobId=${jobId}`),
    saveMeasure: (body: { dimensionId: number; productId: number; value?: number; manualResult?: boolean; isFinal?: boolean; note?: string; measureStage?: number }) =>
      request<unknown>('/api/v1/fai/measure', { method: 'POST', body: JSON.stringify(body) }),
    qcFinalProgress: (productId: number) =>
      request<{ totalDim: number; completeDim: number; passDim: number; failDim: number }>(`/api/v1/products/${productId}/qcfinal-progress`),
  },
  dimensions: {
    list: (opId: number) => request<DimensionDto[]>(`/api/v1/operations/${opId}/dimensions`),
    spc: (opId: number, dimId: number) => request<SpcDto>(`/api/v1/operations/${opId}/dimensions/${dimId}/spc`),
    update: (id: number, body: { nominalValue: number | null; tolerancePlus: number | null; toleranceMinus: number | null }) =>
      request<DimensionDto>(`/api/v1/dimensions/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    review: (id: number, body: { approve: boolean; note?: string }) =>
      request<DimensionDto>(`/api/v1/dimensions/${id}/review`, { method: 'PUT', body: JSON.stringify(body) }),
  },
  ncrs: {
    list: (page = 1, status?: string, jobId?: number) =>
      request<NcrDto[]>(`/api/v1/ncrs?page=${page}&pageSize=20${status ? `&status=${status}` : ''}${jobId ? `&jobId=${jobId}` : ''}`),
    get: (id: number) => request<NcrDetailDto>(`/api/v1/ncrs/${id}`),
    create: (body: { jobId: number; productId?: number; partOpId?: number; description: string }) =>
      request<NcrDto>('/api/v1/ncrs', { method: 'POST', body: JSON.stringify(body) }),
    addAction: (id: number, action: string, note?: string) =>
      request<NcrLogDto>(`/api/v1/ncrs/${id}/actions`, { method: 'POST', body: JSON.stringify({ action, note }) }),
  },
  parts: {
    list: (page = 1, search?: string) =>
      request<PartDto[]>(`/api/v1/parts?page=${page}&pageSize=20${search ? `&search=${encodeURIComponent(search)}` : ''}`),
    create: (body: { partNumber: string; description: string; revCode?: string }) =>
      request<PartRevDto>('/api/v1/parts', { method: 'POST', body: JSON.stringify(body) }),
    revisions: (partId: number) =>
      request<PartRevDto[]>(`/api/v1/parts/${partId}/revisions`),
    addRevision: (partId: number, body: { revCode: string; description?: string }) =>
      request<PartRevDto>(`/api/v1/parts/${partId}/revisions`, { method: 'POST', body: JSON.stringify(body) }),
    // routingId optional — omit to auto-discover via PartRevId (no hardcode needed)
    routingRevs: (partRevId: number, routingId?: number) =>
      request<RoutingRevDto[]>(`/api/v1/parts/revisions/${partRevId}/routing-revs${routingId ? `?routingId=${routingId}` : ''}`),
    addRoutingRev: (body: { routingId: number; revCode: string; changeNote?: string }) =>
      request<RoutingRevDto>('/api/v1/parts/routing-revs', { method: 'POST', body: JSON.stringify(body) }),
  },
  routingRevs: {
    dimensions: (id: number) => request<RoutingRevDimensionDto[]>(`/api/v1/routing-revs/${id}/dimensions`),
    importBulkDimensions: (id: number, file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      return requestMultipart<ImportResultDto>(`/api/v1/routing-revs/${id}/dimensions/import-bulk`, formData)
    },
    importBulkDimensionsTemplate: (id: number) => requestBlob(`/api/v1/routing-revs/${id}/dimensions/import-bulk/template`),
    reviewBatchDimensions: (id: number, body: { approve: boolean; note?: string }) =>
      request<number>(`/api/v1/routing-revs/${id}/dimensions/review-batch`, { method: 'POST', body: JSON.stringify(body) }),
  },
  operations: {
    create: (body: { routingRevId?: number; jobId?: number; opNumber: string; opTypeId?: number; description?: string; note?: string; setupTime?: number; prodTime?: number }) =>
      request<PartOpDto>('/api/v1/operations', { method: 'POST', body: JSON.stringify(body) }),
    listForRoutingRev: (routingRevId: number) =>
      request<PartOpDto[]>(`/api/v1/operations?routingRevId=${routingRevId}`),
    dimensionDefinitions: (opId: number) =>
      request<DimensionDto[]>(`/api/v1/operations/${opId}/dimensions/definitions`),
    createDimension: (opId: number, body: { balloonNumber: string; code?: string; description?: string; nominal: number; upperTol: number; lowerTol: number; unit: string; isCritical: boolean; sortOrder: number }) =>
      request<DimensionDto>(`/api/v1/operations/${opId}/dimensions`, { method: 'POST', body: JSON.stringify(body) }),
    spc: (opId: number, dimId: number) =>
      request<SpcDto>(`/api/v1/operations/${opId}/dimensions/${dimId}/spc`),
    importOps: (routingRevId: number, file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      formData.append('routingRevId', String(routingRevId))
      return requestMultipart<ImportResultDto>('/api/v1/operations/import', formData)
    },
    importDimensions: (opId: number, file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      return requestMultipart<ImportResultDto>(`/api/v1/operations/${opId}/dimensions/import`, formData)
    },
    importOpsTemplate: () => requestBlob('/api/v1/operations/import/template'),
    importDimsTemplate: () => requestBlob('/api/v1/operations/dimensions/import/template'),
  },
  opTypes: {
    list:   (activeOnly = false) => request<OpTypeDto[]>(`/api/v1/op-types?activeOnly=${activeOnly}`),
    create: (body: { code: string; name?: string | null; description?: string | null; isActive: boolean }) =>
      request<OpTypeDto>('/api/v1/op-types', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: { id: number; code: string; name?: string | null; description?: string | null; isActive: boolean }) =>
      request<OpTypeDto>(`/api/v1/op-types/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  },
  dimCategories: {
    list:   (activeOnly = false) => request<DimensionCategoryDto[]>(`/api/v1/dimension-categories?activeOnly=${activeOnly}`),
    create: (body: { code: string; name: string; description?: string | null; isActive: boolean }) =>
      request<DimensionCategoryDto>('/api/v1/dimension-categories', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: { id: number; code: string; name: string; description?: string | null; isActive: boolean }) =>
      request<DimensionCategoryDto>(`/api/v1/dimension-categories/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  },
  fileTypes2: {
    list:   (activeOnly = false) => request<FileTypeDto[]>(`/api/v1/tech-documents/file-types?activeOnly=${activeOnly}`),
    create: (body: Omit<FileTypeDto, 'id'>) =>
      request<FileTypeDto>('/api/v1/tech-documents/file-types', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: FileTypeDto) =>
      request<FileTypeDto>(`/api/v1/tech-documents/file-types/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  },
  machineGroups: {
    list:   (activeOnly = false) => request<MachineGroupDto[]>(`/api/v1/machine-groups?activeOnly=${activeOnly}`),
    create: (body: { code: string; name: string; isActive: boolean }) =>
      request<MachineGroupDto>('/api/v1/machine-groups', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: { id: number; code: string; name: string; isActive: boolean }) =>
      request<MachineGroupDto>(`/api/v1/machine-groups/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  },
  dashboard: {
    overview:   () => request<unknown>('/api/v1/dashboard/overview'),
    production: () => request<unknown>('/api/v1/dashboard/production'),
    quality:    (days = 30) => request<unknown>(`/api/v1/dashboard/quality?days=${days}`),
  },
  machines: {
    list:       (activeOnly = true) => request<MachineDto[]>(`/api/v1/machines?activeOnly=${activeOnly}`),
    status:     () => request<unknown[]>('/api/v1/machines/status'),
    statusLive: (code: string) => request<unknown>(`/api/v1/machines/${code}/status-live`),
    events:     (code: string, date?: string) => request<unknown[]>(`/api/v1/machines/${code}/events${date ? `?date=${date}` : ''}`),
    create: (body: { code: string; name: string; machineType?: string | null; isCnc: boolean; isActive: boolean; serialNumber?: string | null }) =>
      request<MachineDto>('/api/v1/machines', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: { id: number; code: string; name: string; machineType?: string | null; isCnc: boolean; isActive: boolean; serialNumber?: string | null }) =>
      request<MachineDto>(`/api/v1/machines/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  },
  gages: {
    list: (params?: { search?: string; statusCode?: string; isBorrowed?: boolean }) => {
      const q = new URLSearchParams()
      if (params?.search)     q.set('search',     params.search)
      if (params?.statusCode) q.set('statusCode', params.statusCode)
      if (params?.isBorrowed != null) q.set('isBorrowed', String(params.isBorrowed))
      return request<GageDto[]>(`/api/v1/gages?${q}`)
    },
    calibDue: (days = 60) => request<GageDto[]>(`/api/v1/gages/calib-due?days=${days}`),
    create:   (body: CreateGageBody) => request<GageDto>('/api/v1/gages', { method: 'POST', body: JSON.stringify(body) }),
    types:    (categoryId?: number) => request<GageTypeDto[]>(`/api/v1/gage-types${categoryId ? `?categoryId=${categoryId}` : ''}`),
    locations: () => request<GageLocationDto[]>('/api/v1/gage-locations'),
    borrow:   (body: BorrowBody) => request<number>('/api/v1/borrow-transactions', { method: 'POST', body: JSON.stringify(body) }),
    returnGage: (id: number)  => request<null>(`/api/v1/borrow-transactions/${id}/return`, { method: 'PUT', body: '{}' }),
    activeBorrow: (gageId: number) =>
      request<BorrowTransactionDto[]>(`/api/v1/borrow-transactions?gageId=${gageId}&status=0`),
  },
  techDocuments: {
    list: (params?: { status?: string; fileTypeCode?: string; page?: number; partRevId?: number; partOpId?: number; jobId?: number }) => {
      const q = new URLSearchParams({ page: String(params?.page ?? 1), pageSize: '50' })
      if (params?.status)       q.set('status', params.status)
      if (params?.fileTypeCode) q.set('fileTypeCode', params.fileTypeCode)
      if (params?.partRevId)    q.set('partRevId', String(params.partRevId))
      if (params?.partOpId)     q.set('partOpId', String(params.partOpId))
      if (params?.jobId)        q.set('jobId', String(params.jobId))
      return request<TechDocListDto[]>(`/api/v1/tech-documents?${q}`)
    },
    inspect: (id: number, action: 'approve' | 'reject', note?: string) =>
      request<null>(`/api/v1/tech-documents/${id}/inspect`, {
        method: 'PUT', body: JSON.stringify({ approve: action === 'approve', note }),
      }),
    downloadUrl: (id: number) => request<string>(`/api/v1/tech-documents/${id}/download-url`),
    fileTypes: () => request<FileTypeDto[]>('/api/v1/tech-documents/file-types'),
    create: (body: UploadDocBody) =>
      request<UploadResponseDto>('/api/v1/tech-documents', { method: 'POST', body: JSON.stringify(body) }),
    resolveBatch: (items: ResolveBatchItem[]) =>
      request<ResolveBatchResultDto[]>('/api/v1/tech-documents/resolve-batch', { method: 'POST', body: JSON.stringify(items) }),
  },
  planning: {
    items: (params?: { startDate?: string; endDate?: string; machineId?: number }) => {
      const q = new URLSearchParams()
      if (params?.startDate) q.set('startDate', params.startDate)
      if (params?.endDate)   q.set('endDate',   params.endDate)
      if (params?.machineId) q.set('machineId', String(params.machineId))
      return request<PlanningItemDto[]>(`/api/v1/planning?${q}`)
    },
    create: (body: CreatePlanningItemBody) =>
      request<PlanningItemDto>('/api/v1/planning', { method: 'POST', body: JSON.stringify(body) }),
    delete: (id: number) =>
      request<null>(`/api/v1/planning/${id}`, { method: 'DELETE' }),
    shifts:     () => request<ShiftDto[]>('/api/v1/shifts'),
    breakTimes: () => request<BreakTimeDto[]>('/api/v1/break-times'),
  },
  erp: {
    connections: () => request<ErpConnectionDto[]>('/api/v1/erp/connections'),
    createConnection: (body: { name: string; erpType: string; baseUrl: string; company?: string; username?: string; password?: string }) =>
      request<ErpConnectionDto>('/api/v1/erp/connections', { method: 'POST', body: JSON.stringify(body) }),
    updateConnection: (id: number, body: { name: string; erpType: string; baseUrl: string; company?: string; username?: string; password?: string; isActive: boolean }) =>
      request<ErpConnectionDto>(`/api/v1/erp/connections/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
    testConnection: (id: number) =>
      request<boolean>(`/api/v1/erp/connections/${id}/test`, { method: 'POST', body: '{}' }),
    preview: (body: { connectionId: number; dateFrom?: string; dateTo?: string; poNumber?: string }) =>
      request<ErpPreviewDto>('/api/v1/erp/preview', { method: 'POST', body: JSON.stringify(body) }),
    import: (body: { connectionId: number; dateFrom?: string; dateTo?: string; poNumber?: string }) =>
      request<GlobalImportResultDto>('/api/v1/erp/import', { method: 'POST', body: JSON.stringify(body) }),
  },
  calibration: {
    vendors:        () => request<CalibVendorDto[]>('/api/v1/calib-vendors'),
    createVendor:   (body: { name: string; contact?: string; phone?: string; email?: string }) =>
      request<CalibVendorDto>('/api/v1/calib-vendors', { method: 'POST', body: JSON.stringify(body) }),
    requests:       (params?: { status?: string; gageId?: number }) => {
      const q = new URLSearchParams()
      if (params?.status) q.set('status', params.status)
      if (params?.gageId) q.set('gageId', String(params.gageId))
      return request<CalibRequestDto[]>(`/api/v1/calib-requests?${q}`)
    },
    createRequest:  (body: { gageId: number; vendorId?: number }) =>
      request<number>('/api/v1/calib-requests', { method: 'POST', body: JSON.stringify(body) }),
    approveRequest: (id: number) =>
      request<null>(`/api/v1/calib-requests/${id}/approve`, { method: 'PUT', body: '{}' }),
    complete:       (body: CompleteCalibBody) =>
      request<number>('/api/v1/calib-records', { method: 'POST', body: JSON.stringify(body) }),
  },
}

// ── Gage types ────────────────────────────────────────────────────────────
export type GageDto = {
  id: number; gageNo: string; serialNo: string | null
  description: string; measuringRange: string | null; accuracy: string | null; unit: string
  manufacturer: string | null; calibFrequencyDays: number | null
  lastCalibration: string | null; dueDate: string | null; daysRemaining: number | null
  inServiceDate: string | null
  statusCode: string; isValid: boolean
  gageTypeId: number | null; gageTypeName: string | null; categoryCode: string | null
  currentLocationId: number | null; currentLocationDesc: string | null
  isBorrowed: boolean; hasPendingCalib: boolean; note: string | null
}

export type GageTypeDto     = { id: number; code: string; name: string; categoryCode: string | null }
export type GageLocationDto = { id: number; code: string; description: string }
export type CalibVendorDto  = { id: number; name: string; contact: string | null; phone: string | null; email: string | null }

export type CalibRequestDto = {
  id: number; gageId: number; gageNo: string; gageDescription: string
  vendorId: number | null; vendorName: string | null
  requestDate: string; status: number
  procedureName: string | null; calibrationDate: string | null
  calibratedBy: string | null; asFoundConditions: string | null
}

export type BorrowTransactionDto = {
  id: number; gageId: number; gageNo: string
  borrowerId: number; borrowerName: string | null
  borrowDate: string; expectedReturnDate: string | null; returnDate: string | null
  status: number; note: string | null
}

export type CreateGageBody = {
  gageNo: string; description: string; serialNo?: string; measuringRange?: string
  accuracy?: string; unit: string; manufacturer?: string; calibFrequencyDays?: number
  lastCalibration?: string; inServiceDate?: string
  gageTypeId?: number; defaultLocationId?: number; vendorId?: number; note?: string
}

export type BorrowBody = {
  gageId: number; borrowerId: number; managerId: number
  expectedReturnDate?: string; useLocationId?: number; note?: string
}

export type PlanningItemDto = {
  id: number; jobId: number; jobNumber: string
  partOpId: number; opNumber: string; opTypeName: string | null
  machineId: number; machineCode: string; machineName: string | null
  operatorId: number | null; operatorName: string | null
  shiftId: number | null; shiftName: string | null
  startTime: string; endTime: string; note: string | null
}

export type ShiftDto     = { id: number; name: string; startTime: string; endTime: string }
export type BreakTimeDto = { id: number; fromTime: string; toTime: string; label: string | null }

export type CreatePlanningItemBody = {
  jobId: number; partOpId: number; machineId: number
  operatorId?: number; shiftId?: number
  startTime: string; endTime: string; note?: string
}

export type TechDocListDto = {
  id: number; fileTypeCode: string; fileTypeName: string
  partRevId: number | null; partOpId: number | null; jobId: number | null
  description: string | null; revision: string | null; code: string | null
  segment: string | null; machineType: string | null
  status: string; createdByName: string; createdAt: string
  storagePath?: string
  fileName: string; partNumber: string | null; drawingRevCode: string | null
  routingRevCode: string | null; opNumber: string | null
  fileSizeBytes: number | null
}

export type FileTypeDto = {
  id: number; code: string; name: string; folder: string | null
  isPartNumber: boolean; isRevision: boolean; isOpNumber: boolean; isJobNumber: boolean
  isGcode: boolean; isSegment: boolean; sortOrder: number; isActive: boolean
}

export type MachineDto = {
  id: number; code: string; name: string; machineType: string | null; isCnc: boolean
  isActive: boolean; serialNumber: string | null
}

export type MachineGroupDto = { id: number; code: string; name: string; isActive: boolean; machineCount: number }
export type OpTypeDto = { id: number; code: string; name: string | null; description: string | null; isActive: boolean }
export type DimensionCategoryDto = { id: number; code: string; name: string; description: string | null; isActive: boolean }

export type UploadDocBody = {
  fileTypeId: number; fileName: string
  partRevId?: number | null; partOpId?: number | null; jobId?: number | null
  description?: string | null; revision?: string | null
  code?: string | null; segment?: string | null; machineType?: string | null
  fileSizeBytes?: number | null
}

export type UploadResponseDto = { documentId: number; objectKey: string; uploadUrl: string }

export type ResolveBatchItem = {
  fileName: string; fileTypeCode: string
  partNumber: string | null; partRevCode: string | null; routingRevCode: string | null
  opNumber: string | null; jobNumber: string | null
  segmentIndex: number | null; segmentTotal: number | null
  fileSizeBytes: number | null
}

export type ResolveBatchResultDto = {
  fileName: string
  status: 'Ready' | 'Invalid'
  reason: string | null
  fileTypeId: number | null
  partRevId: number | null; partOpId: number | null; jobId: number | null
  resolvedPartNumber: string | null; resolvedRevCode: string | null
  resolvedRoutingRevCode: string | null; resolvedOpNumber: string | null; resolvedJobNumber: string | null
  existingSegments: string[] | null
}

export type UserListDto = {
  id: number; userLogin: string; name: string; email: string | null; sex: string | null
  role: string | null; userType: string | null; position: string | null
  roleId: number | null; userTypeId: number | null; positionId: number | null; workStatusId: number | null
  isActive: boolean; firstLogin: boolean; createdAt: string
}

export type PositionDto = { id: number; code: string; description: string | null; isActive: boolean }
export type UserTypeDto = { id: number; typeName: string; description: string | null; canEnterValue: boolean; canRaiseNcr: boolean }
export type WorkStatusDto = { id: number; name: string; isWorking: boolean }
export type DepartmentDto = { id: number; code: string; name: string }

export type CompleteCalibBody = {
  requestId: number; procedureId?: number; calibratedBy?: string
  calibrationDate: string; asFoundConditions?: string
  adjustmentMade?: number; temperature?: number; humidity?: number
  storagePath?: string
}
