import {
  Camera,
  Check,
  KeyRound,
  LoaderCircle,
  Mail,
  ShieldCheck,
  Trash2,
  UserRound,
} from "lucide-react"

import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
} from "react"

import { api } from "../api/client"
import { useAuth } from "../auth/AuthContext"

interface PerfilApi {
  idUtilizador: number
  nome: string
  email: string
  perfil: string
  ativo: boolean
  dataCriacao: string
  ultimoAcesso?: string | null
  fotoPerfil?: string | null
}

function mensagemErro(
  error: unknown,
  fallback: string,
) {
  const e = error as {
    response?: {
      data?: {
        mensagem?: string
      }
    }
  }

  return (
    e.response?.data?.mensagem ??
    fallback
  )
}

function fotoUrl(
  caminho?: string | null,
) {
  if (!caminho) {
    return null
  }

  if (
    caminho.startsWith("http://") ||
    caminho.startsWith("https://")
  ) {
    return caminho
  }

  return `http://localhost:5021${caminho}`
}

export function ProfilePage() {
  const { user, updateUser } = useAuth()

  const fileRef =
    useRef<HTMLInputElement | null>(null)

  const [perfil, setPerfil] =
    useState<PerfilApi | null>(null)

  const [nome, setNome] =
    useState(user?.nome ?? "")

  const [email, setEmail] =
    useState(user?.email ?? "")

  const [loading, setLoading] =
    useState(true)

  const [saving, setSaving] =
    useState(false)

  const [fotoLoading, setFotoLoading] =
    useState(false)

  const [passwordLoading, setPasswordLoading] =
    useState(false)

  const [mensagem, setMensagem] =
    useState<string | null>(null)

  const [erro, setErro] =
    useState<string | null>(null)

  const [passwordAtual, setPasswordAtual] =
    useState("")

  const [novaPassword, setNovaPassword] =
    useState("")

  const [
    confirmarPassword,
    setConfirmarPassword,
  ] = useState("")

  async function carregar() {
    try {
      setLoading(true)
      setErro(null)

      const response =
        await api.get<PerfilApi>("/perfil")

      setPerfil(response.data)
      setNome(response.data.nome)
      setEmail(response.data.email)

      updateUser(response.data)
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel carregar o perfil.",
        ),
      )
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void carregar()
  }, [])

  async function guardar(
    event: FormEvent,
  ) {
    event.preventDefault()

    try {
      setSaving(true)
      setErro(null)
      setMensagem(null)

      const response =
        await api.put("/perfil", {
          nome,
          email,
        })

      const atualizado =
        response.data.utilizador as PerfilApi

      setPerfil(atualizado)
      updateUser(atualizado)

      setMensagem(
        response.data.mensagem ??
          "Perfil atualizado.",
      )
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel atualizar o perfil.",
        ),
      )
    } finally {
      setSaving(false)
    }
  }

  async function alterarFoto(
    file: File,
  ) {
    try {
      setFotoLoading(true)
      setErro(null)
      setMensagem(null)

      const form = new FormData()
      form.append("foto", file)

      const response =
        await api.post(
          "/perfil/foto",
          form,
          {
            headers: {
              "Content-Type":
                "multipart/form-data",
            },
          },
        )

      const atualizado =
        response.data.utilizador as PerfilApi

      setPerfil(atualizado)
      updateUser(atualizado)

      setMensagem(
        "Fotografia atualizada com sucesso.",
      )
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel alterar a fotografia.",
        ),
      )
    } finally {
      setFotoLoading(false)
    }
  }

  async function removerFoto() {
    try {
      setFotoLoading(true)
      setErro(null)
      setMensagem(null)

      const response =
        await api.delete("/perfil/foto")

      const atualizado =
        response.data.utilizador as PerfilApi

      setPerfil(atualizado)
      updateUser(atualizado)

      setMensagem(
        "Fotografia removida.",
      )
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel remover a fotografia.",
        ),
      )
    } finally {
      setFotoLoading(false)
    }
  }

  async function alterarPassword(
    event: FormEvent,
  ) {
    event.preventDefault()

    if (
      novaPassword !==
      confirmarPassword
    ) {
      setErro(
        "A confirmacao da nova password nao coincide.",
      )
      return
    }

    try {
      setPasswordLoading(true)
      setErro(null)
      setMensagem(null)

      const response =
        await api.put(
          "/perfil/password",
          {
            passwordAtual,
            novaPassword,
            confirmarPassword,
          },
        )

      setPasswordAtual("")
      setNovaPassword("")
      setConfirmarPassword("")

      setMensagem(
        response.data.mensagem ??
          "Password alterada.",
      )
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel alterar a password.",
        ),
      )
    } finally {
      setPasswordLoading(false)
    }
  }

  const foto =
    fotoUrl(
      perfil?.fotoPerfil ??
      user?.fotoPerfil,
    )

  const inicial =
    (perfil?.nome ?? user?.nome ?? "D")
      .trim()
      .charAt(0)
      .toUpperCase()

  if (loading) {
    return (
      <div className="df-profile-loading">
        <LoaderCircle
          size={24}
          className="spin"
        />
        A carregar perfil...
      </div>
    )
  }

  return (
    <div className="df-profile-page">

      <header className="df-profile-heading">
        <div>
          <span className="df-kicker">
            CONTA
          </span>

          <h1>Meu perfil</h1>

          <p>
            Gerencie os seus dados pessoais,
            fotografia e segurança da conta.
          </p>
        </div>
      </header>

      {mensagem && (
        <div className="df-profile-success">
          <Check size={17} />
          {mensagem}
        </div>
      )}

      {erro && (
        <div className="df-profile-error">
          {erro}
        </div>
      )}

      <div className="df-profile-grid">

        <aside className="df-profile-card df-profile-photo-card">

          <div className="df-profile-avatar-large">
            {foto ? (
              <img
                src={foto}
                alt={perfil?.nome ?? "Perfil"}
              />
            ) : (
              <span>{inicial}</span>
            )}
          </div>

          <h2>{perfil?.nome}</h2>
          <p>{perfil?.email}</p>

          <span className="df-profile-role">
            {perfil?.perfil}
          </span>

          <input
            ref={fileRef}
            type="file"
            accept=".jpg,.jpeg,.png,.webp"
            hidden
            onChange={(event) => {
              const file =
                event.target.files?.[0]

              if (file) {
                void alterarFoto(file)
              }

              event.currentTarget.value = ""
            }}
          />

          <button
            type="button"
            className="df-primary-button df-profile-full-button"
            disabled={fotoLoading}
            onClick={() =>
              fileRef.current?.click()
            }
          >
            {fotoLoading ? (
              <LoaderCircle
                size={16}
                className="spin"
              />
            ) : (
              <Camera size={16} />
            )}

            Alterar fotografia
          </button>

          {foto && (
            <button
              type="button"
              className="df-profile-delete-photo"
              disabled={fotoLoading}
              onClick={() =>
                void removerFoto()
              }
            >
              <Trash2 size={15} />
              Remover fotografia
            </button>
          )}

          <small>
            JPG, PNG ou WEBP · máximo 5 MB
          </small>

        </aside>

        <div className="df-profile-main">

          <section className="df-profile-card">

            <div className="df-profile-section-title">
              <div className="df-profile-section-icon">
                <UserRound size={18} />
              </div>

              <div>
                <h2>Informações pessoais</h2>
                <p>
                  Dados utilizados na sua conta
                  DataForge.
                </p>
              </div>
            </div>

            <form
              className="df-profile-form"
              onSubmit={guardar}
            >

              <label className="df-field">
                <span>Nome</span>

                <div className="df-profile-input-icon">
                  <UserRound size={16} />

                  <input
                    value={nome}
                    maxLength={150}
                    onChange={(event) =>
                      setNome(event.target.value)
                    }
                    required
                  />
                </div>
              </label>

              <label className="df-field">
                <span>Email</span>

                <div className="df-profile-input-icon">
                  <Mail size={16} />

                  <input
                    type="email"
                    value={email}
                    maxLength={200}
                    onChange={(event) =>
                      setEmail(event.target.value)
                    }
                    required
                  />
                </div>
              </label>

              <label className="df-field">
                <span>Perfil</span>

                <div className="df-profile-input-icon">
                  <ShieldCheck size={16} />

                  <input
                    value={
                      perfil?.perfil ??
                      "Analista"
                    }
                    disabled
                  />
                </div>
              </label>

              <div className="df-profile-form-actions">
                <button
                  type="submit"
                  className="df-primary-button"
                  disabled={saving}
                >
                  {saving ? (
                    <LoaderCircle
                      size={16}
                      className="spin"
                    />
                  ) : (
                    <Check size={16} />
                  )}

                  Guardar alterações
                </button>
              </div>

            </form>

          </section>

          <section
            className="df-profile-card"
            id="seguranca"
          >

            <div className="df-profile-section-title">
              <div className="df-profile-section-icon">
                <KeyRound size={18} />
              </div>

              <div>
                <h2>Segurança</h2>
                <p>
                  Altere a password da sua conta.
                </p>
              </div>
            </div>

            <form
              className="df-profile-form"
              onSubmit={alterarPassword}
            >

              <label className="df-field">
                <span>Password atual</span>

                <input
                  type="password"
                  autoComplete="current-password"
                  value={passwordAtual}
                  onChange={(event) =>
                    setPasswordAtual(
                      event.target.value,
                    )
                  }
                  required
                />
              </label>

              <div className="df-profile-password-grid">

                <label className="df-field">
                  <span>Nova password</span>

                  <input
                    type="password"
                    autoComplete="new-password"
                    minLength={8}
                    value={novaPassword}
                    onChange={(event) =>
                      setNovaPassword(
                        event.target.value,
                      )
                    }
                    required
                  />
                </label>

                <label className="df-field">
                  <span>
                    Confirmar nova password
                  </span>

                  <input
                    type="password"
                    autoComplete="new-password"
                    minLength={8}
                    value={confirmarPassword}
                    onChange={(event) =>
                      setConfirmarPassword(
                        event.target.value,
                      )
                    }
                    required
                  />
                </label>

              </div>

              <div className="df-password-note">
                <ShieldCheck size={16} />

                <span>
                  A nova password deve possuir
                  pelo menos 8 caracteres.
                </span>
              </div>

              <div className="df-profile-form-actions">
                <button
                  type="submit"
                  className="df-primary-button"
                  disabled={passwordLoading}
                >
                  {passwordLoading ? (
                    <LoaderCircle
                      size={16}
                      className="spin"
                    />
                  ) : (
                    <KeyRound size={16} />
                  )}

                  Alterar password
                </button>
              </div>

            </form>

          </section>

        </div>

      </div>

    </div>
  )
}
