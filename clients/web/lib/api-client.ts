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

// ── Types ─────────────────────────────────────────────────────

export type PartDto = { id: number; partNumber: string; description: string; createdAt: string }

export type PartRevDto = {
  id: number; partId: number; partNumber: string; revCode: string
  description: string | null; isActive: boolean; isReleased: boolean; createdAt: string
}

export type RoutingRevDto = {
  id: number; routingId: number; revCode: string; changeNote: string | null
  isActive: boolean; isReleased: boolean; createdAt: string; opCount: number
}

export type JobDto = {
  id: number; jobNumber: string
  partRevId: number; partNumber: string; revCode: string
  routingRevId: number; routingRevCode: string
  runQty: number | null; completedCount: number; shipBy: string | null; isComplete: boolean; createdAt: string
}

export type PartOpDto = {
  id: number; routingRevId: number | null; jobId: number | null; forJobOnly: boolean
  opNumber: string; opNumberSort: number | null
  opTypeId: number | null; opTypeName: string | null
  description: string | null; note: string | null
  setupTime: number | null; prodTime: number | null; isVisible: boolean; isComplete: boolean
}

export type JobDetailDto = JobDto & {
  partDescription: string
  operations: PartOpDto[]
  products: ProductDto[]
}

export type ProductDto = { id: number; serialNumber: string; jobId: number; isComplete: boolean; sortOrder: number | null }

export type DimensionDto = {
  id: number; partOpId: number
  balloonNumber: string; code: string | null; description: string | null
  nominal: number; upperTol: number; lowerTol: number
  upperLimit: number; lowerLimit: number; unit: string
  isCritical: boolean; sortOrder: number
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
    generateProducts: (id: number, quantity: number) =>
      request<ProductDto[]>(`/api/v1/jobs/${id}/products/generate`, {
        method: 'POST', body: JSON.stringify({ quantity }),
      }),
  },
  lookups: {
    roles: () => request<{ id: number; name: string }[]>('/api/v1/roles'),
    departments: () => request<{ id: number; code: string; name: string }[]>('/api/v1/departments'),
  },
  fai: {
    sheet: (partOpId: number, jobId: number) =>
      request<FaiSheetDto>(`/api/v1/fai?partOpId=${partOpId}&jobId=${jobId}`),
    saveMeasure: (body: { dimensionId: number; productId: number; value: number; note?: string }) =>
      request<unknown>('/api/v1/fai/measure', { method: 'POST', body: JSON.stringify(body) }),
  },
  dimensions: {
    list: (opId: number) => request<DimensionDto[]>(`/api/v1/operations/${opId}/dimensions`),
    spc: (opId: number, dimId: number) => request<SpcDto>(`/api/v1/operations/${opId}/dimensions/${dimId}/spc`),
  },
  ncrs: {
    list: (page = 1, status?: string) =>
      request<NcrDto[]>(`/api/v1/ncrs?page=${page}&pageSize=20${status ? `&status=${status}` : ''}`),
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
  operations: {
    create: (body: { routingRevId?: number; jobId?: number; opNumber: string; opTypeId?: number; description?: string; note?: string; setupTime?: number; prodTime?: number }) =>
      request<PartOpDto>('/api/v1/operations', { method: 'POST', body: JSON.stringify(body) }),
    listForRoutingRev: (routingRevId: number) =>
      request<PartOpDto[]>(`/api/v1/operations?routingRevId=${routingRevId}`),
    createDimension: (opId: number, body: { balloonNumber: string; code?: string; description?: string; nominal: number; upperTol: number; lowerTol: number; unit: string; isCritical: boolean; sortOrder: number }) =>
      request<DimensionDto>(`/api/v1/operations/${opId}/dimensions`, { method: 'POST', body: JSON.stringify(body) }),
    spc: (opId: number, dimId: number) =>
      request<SpcDto>(`/api/v1/operations/${opId}/dimensions/${dimId}/spc`),
  },
  opTypes: {
    list: () => request<{ id: number; code: string; name: string | null }[]>('/api/v1/op-types'),
  },
  dimCategories: {
    list: () => request<{ id: number; code: string; name: string; description: string | null }[]>('/api/v1/dimension-categories'),
  },
  fileTypes2: {
    list: () => request<{ id: number; code: string; name: string; folder: string; isPartNumber: boolean; isOpNumber: boolean; isJobNumber: boolean }[]>('/api/v1/tech-documents/file-types'),
  },
  machineGroups: {
    list: () => request<{ id: number; code: string; name: string; machineCount: number }[]>('/api/v1/machine-groups'),
  },
  dashboard: {
    overview:   () => request<unknown>('/api/v1/dashboard/overview'),
    production: () => request<unknown>('/api/v1/dashboard/production'),
    quality:    (days = 30) => request<unknown>(`/api/v1/dashboard/quality?days=${days}`),
  },
  machines: {
    list:       (activeOnly = true) => request<{ id: number; code: string; name: string; machineType: string | null }[]>(`/api/v1/machines?activeOnly=${activeOnly}`),
    status:     () => request<unknown[]>('/api/v1/machines/status'),
    statusLive: (code: string) => request<unknown>(`/api/v1/machines/${code}/status-live`),
    events:     (code: string, date?: string) => request<unknown[]>(`/api/v1/machines/${code}/events${date ? `?date=${date}` : ''}`),
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
    list: (params?: { status?: string; fileTypeCode?: string; page?: number }) => {
      const q = new URLSearchParams({ page: String(params?.page ?? 1), pageSize: '50' })
      if (params?.status)       q.set('status', params.status)
      if (params?.fileTypeCode) q.set('fileTypeCode', params.fileTypeCode)
      return request<TechDocListDto[]>(`/api/v1/tech-documents?${q}`)
    },
    inspect: (id: number, action: 'approve' | 'reject', note?: string) =>
      request<null>(`/api/v1/tech-documents/${id}/inspect`, {
        method: 'PUT', body: JSON.stringify({ action, note }),
      }),
    downloadUrl: (id: number) => request<string>(`/api/v1/tech-documents/${id}/download-url`),
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
}

export type UserListDto = {
  id: number; userLogin: string; name: string; email: string | null
  role: string | null; userType: string | null; position: string | null
  isActive: boolean; firstLogin: boolean; createdAt: string
}

export type CompleteCalibBody = {
  requestId: number; procedureId?: number; calibratedBy?: string
  calibrationDate: string; asFoundConditions?: string
  adjustmentMade?: number; temperature?: number; humidity?: number
  storagePath?: string
}
