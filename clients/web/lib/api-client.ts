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
  runQty: number | null; shipBy: string | null; isComplete: boolean; createdAt: string
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
  id: number; ncrNumber: string; jobId: number; jobNumber: string
  description: string; status: string
  raisedBy: string; raisedAt: string
  closedBy: string | null; closedAt: string | null
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
    changePassword: (currentPassword: string, newPassword: string) =>
      request<null>('/api/v1/users/me/change-password', {
        method: 'POST', body: JSON.stringify({ currentPassword, newPassword }),
      }),
  },
  parts: {
    list: (page = 1, search?: string) =>
      request<PartDto[]>(`/api/v1/parts?page=${page}&pageSize=20${search ? `&search=${encodeURIComponent(search)}` : ''}`),
    create: (body: { partNumber: string; description: string; revCode?: string }) =>
      request<PartRevDto>('/api/v1/parts', { method: 'POST', body: JSON.stringify(body) }),
    revisions: (partId: number) =>
      request<PartRevDto[]>(`/api/v1/parts/${partId}/revisions`),
    routingRevs: (partRevId: number, routingId: number) =>
      request<RoutingRevDto[]>(`/api/v1/parts/revisions/${partRevId}/routing-revs?routingId=${routingId}`),
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
    get: (id: number) => request<{ ncr: NcrDto; logs: unknown[] }>(`/api/v1/ncrs/${id}`),
    create: (body: { jobId: number; productId?: number; partOpId?: number; description: string }) =>
      request<NcrDto>('/api/v1/ncrs', { method: 'POST', body: JSON.stringify(body) }),
    addAction: (id: number, action: string, note?: string) =>
      request<unknown>(`/api/v1/ncrs/${id}/actions`, { method: 'POST', body: JSON.stringify({ action, note }) }),
  },
}
