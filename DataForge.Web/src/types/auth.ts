export interface Utilizador {
  fotoPerfil?: string | null
  idUtilizador: number
  nome: string
  email: string
  perfil: string
  ativo: boolean
}

export interface AuthResponse {
  token: string
  utilizador: Utilizador
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegistoRequest {
  nome: string
  email: string
  password: string
}


