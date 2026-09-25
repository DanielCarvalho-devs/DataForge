import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react"

import { api } from "../api/client"

import type {
  AuthResponse,
  LoginRequest,
  RegistoRequest,
  Utilizador,
} from "../types/auth"

interface AuthContextValue {
  user: Utilizador | null
  authenticated: boolean
  login: (
    dados: LoginRequest,
    manterSessao: boolean,
  ) => Promise<void>
  register: (dados: RegistoRequest) => Promise<void>
  updateUser: (dados: Partial<Utilizador>) => void
  logout: () => void
}

const TOKEN_KEY = "dataforge_token"
const USER_KEY = "dataforge_user"

const AuthContext =
  createContext<AuthContextValue | undefined>(undefined)

function getStoredValue(key: string) {
  return (
    localStorage.getItem(key) ??
    sessionStorage.getItem(key)
  )
}

function getStoredUser(): Utilizador | null {
  const stored = getStoredValue(USER_KEY)

  if (!stored) {
    return null
  }

  try {
    return JSON.parse(stored) as Utilizador
  } catch {
    localStorage.removeItem(USER_KEY)
    sessionStorage.removeItem(USER_KEY)
    return null
  }
}

function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)

  sessionStorage.removeItem(TOKEN_KEY)
  sessionStorage.removeItem(USER_KEY)
}

function saveSession(
  auth: AuthResponse,
  persistente: boolean,
) {
  clearSession()

  const storage = persistente
    ? localStorage
    : sessionStorage

  storage.setItem(TOKEN_KEY, auth.token)

  storage.setItem(
    USER_KEY,
    JSON.stringify(auth.utilizador),
  )
}

export function getAuthToken() {
  return getStoredValue(TOKEN_KEY)
}

export function AuthProvider({
  children,
}: {
  children: ReactNode
}) {
  const [user, setUser] = useState<Utilizador | null>(
    getStoredUser,
  )

  async function login(
    dados: LoginRequest,
    manterSessao: boolean,
  ) {
    const response = await api.post<AuthResponse>(
      "/auth/login",
      dados,
    )

    saveSession(
      response.data,
      manterSessao,
    )

    setUser(response.data.utilizador)
  }

  async function register(
    dados: RegistoRequest,
  ) {
    const response = await api.post<AuthResponse>(
      "/auth/registar",
      dados,
    )

    saveSession(response.data, true)
    setUser(response.data.utilizador)
  }

  function updateUser(
    dados: Partial<Utilizador>,
  ) {
    setUser((atual) => {
      if (!atual) {
        return atual
      }

      const novo = {
        ...atual,
        ...dados,
      }

      const storage =
        localStorage.getItem(TOKEN_KEY)
          ? localStorage
          : sessionStorage

      storage.setItem(
        USER_KEY,
        JSON.stringify(novo),
      )

      return novo
    })
  }

  function logout() {
    clearSession()
    setUser(null)
  }

  const value = useMemo(
    () => ({
      user,
      authenticated: Boolean(
        user && getStoredValue(TOKEN_KEY),
      ),
      login,
      register,
      updateUser,
      logout,
    }),
    [user],
  )

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error(
      "useAuth precisa estar dentro de AuthProvider.",
    )
  }

  return context
}

