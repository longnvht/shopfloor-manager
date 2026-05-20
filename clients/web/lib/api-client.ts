const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5066'

type ApiResponse<T> = {
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
        method: 'POST',
        body: JSON.stringify({ email, code, newPassword }),
      }),
  },
  users: {
    me: (id: number) => request<{ id: number; name: string; role: string }>(`/api/v1/users/${id}`),
    changePassword: (currentPassword: string, newPassword: string) =>
      request<null>('/api/v1/users/me/change-password', {
        method: 'POST',
        body: JSON.stringify({ currentPassword, newPassword }),
      }),
  },
}
