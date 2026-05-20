'use client'

import { create } from 'zustand'
import { persist } from 'zustand/middleware'

type AuthUser = {
  id: number
  name: string
  role: string
  firstLogin: boolean
}

type AuthState = {
  token: string | null
  user: AuthUser | null
  isAuthenticated: boolean
  login: (token: string, user: AuthUser) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      isAuthenticated: false,
      login: (token, user) => {
        // Also set cookie so middleware can read it
        document.cookie = `auth-token=${token}; path=/; max-age=${8 * 60 * 60}; samesite=strict`
        localStorage.setItem('auth-token', token)
        set({ token, user, isAuthenticated: true })
      },
      logout: () => {
        document.cookie = 'auth-token=; path=/; max-age=0'
        localStorage.removeItem('auth-token')
        set({ token: null, user: null, isAuthenticated: false })
      },
    }),
    { name: 'auth-storage', partialize: (s) => ({ token: s.token, user: s.user, isAuthenticated: s.isAuthenticated }) }
  )
)
