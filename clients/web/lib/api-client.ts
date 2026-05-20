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

export type PartDto = {
  id: number; partNumber: string; description: string
  revision: string | null; routingRevision: string | null
  isActive: boolean; isComplete: boolean; status: number; createdAt: string
}

export type JobDto = {
  id: number; jobNumber: string; partId: number
  partNumber: string; partDescription: string; partRevision: string | null
  runQty: number | null; shipBy: string | null; createdAt: string
}

export type JobDetailDto = JobDto & {
  operations: { id: number; opNumber: string; opTypeName: string | null; description: string | null; isComplete: boolean }[]
  products: { id: number; serialNumber: string; isComplete: boolean }[]
}

export type PartOpDto = {
  id: number; opNumber: string; opTypeName: string | null
  description: string | null; isComplete: boolean; setupTime: number | null; prodTime: number | null
}

export type ProductDto = { id: number; serialNumber: string; jobId: number; isComplete: boolean }

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
    get: (id: number) => request<PartDto>(`/api/v1/parts/${id}`),
    create: (body: { partNumber: string; description: string; revision?: string }) =>
      request<PartDto>('/api/v1/parts', { method: 'POST', body: JSON.stringify(body) }),
    update: (id: number, body: { description: string; revision?: string; routingRevision?: string; isActive: boolean }) =>
      request<PartDto>(`/api/v1/parts/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  },
  jobs: {
    list: (page = 1, search?: string) =>
      request<JobDto[]>(`/api/v1/jobs?page=${page}&pageSize=20${search ? `&search=${encodeURIComponent(search)}` : ''}`),
    get: (id: number) => request<JobDetailDto>(`/api/v1/jobs/${id}`),
    create: (body: { jobNumber: string; partId: number; runQty?: number; shipBy?: string }) =>
      request<JobDto>('/api/v1/jobs', { method: 'POST', body: JSON.stringify(body) }),
    generateProducts: (id: number, quantity: number) =>
      request<ProductDto[]>(`/api/v1/jobs/${id}/products/generate`, {
        method: 'POST', body: JSON.stringify({ quantity }),
      }),
  },
  lookups: {
    roles: () => request<{ id: number; name: string }[]>('/api/v1/roles'),
    departments: () => request<{ id: number; code: string; name: string }[]>('/api/v1/departments'),
  },
}
