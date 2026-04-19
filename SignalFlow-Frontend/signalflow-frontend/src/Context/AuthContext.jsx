/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { login as loginUser, register as registerUser } from '../Service/AuthService.js'

const AUTH_STORAGE_KEY = 'signalflow-auth'

const AuthContext = createContext(null)

const getStoredAuth = () => {
  if (typeof window === 'undefined') {
    return { token: null, user: null }
  }

  try {
    const storedValue = window.localStorage.getItem(AUTH_STORAGE_KEY)
    return storedValue ? JSON.parse(storedValue) : { token: null, user: null }
  } catch {
    return { token: null, user: null }
  }
}

const AuthProvider = ({ children }) => {
  const storedAuth = getStoredAuth()
  const [token, setToken] = useState(storedAuth.token)
  const [user, setUser] = useState(storedAuth.user)

  useEffect(() => {
    if (typeof window === 'undefined') {
      return
    }

    if (token && user) {
      window.localStorage.setItem(
        AUTH_STORAGE_KEY,
        JSON.stringify({ token, user }),
      )
      return
    }

    window.localStorage.removeItem(AUTH_STORAGE_KEY)
  }, [token, user])

  const setSession = useCallback((session) => {
    const nextUser = {
      id: session.id ?? null,
      email: session.email ?? '',
      username: session.username ?? '',
      refreshTokenExpiryTime: session.refreshTokenExpiryTime ?? null,
    }

    setUser(nextUser)
    setToken(session.token ?? null)
  }, [])

  const logout = useCallback(() => {
    setUser(null)
    setToken(null)
  }, [])

  const register = useCallback(
    async (payload) => {
      const session = await registerUser(payload)
      setSession(session)
      return session
    },
    [setSession],
  )

  const login = useCallback(
    async (payload) => {
      const session = await loginUser(payload)
      setSession(session)
      return session
    },
    [setSession],
  )

  const value = useMemo(
    () => ({
      isAuthenticated: Boolean(token),
      login,
      logout,
      register,
      setSession,
      token,
      user,
    }),
    [login, logout, register, setSession, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

const useAuth = () => {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider')
  }

  return context
}

export { AuthContext, AuthProvider, useAuth }
