import axios from "axios"

import {
  ArrowLeft,
  ArrowRight,
  Database,
  Eye,
  EyeOff,
  LockKeyhole,
  Mail,
  User,
  UserPlus,
} from "lucide-react"

import {
  useState,
  type FormEvent,
} from "react"

import {
  Link,
  Navigate,
  useNavigate,
} from "react-router-dom"

import { useAuth } from "../auth/AuthContext"

export function RegisterPage() {
  const {
    register,
    authenticated,
  } = useAuth()

  const navigate = useNavigate()

  const [nome, setNome] = useState("")
  const [email, setEmail] = useState("")
  const [password, setPassword] =
    useState("")

  const [confirmPassword, setConfirmPassword] =
    useState("")

  const [showPassword, setShowPassword] =
    useState(false)

  const [loading, setLoading] =
    useState(false)

  const [error, setError] = useState("")

  if (authenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    const normalizedName = nome.trim()
    const normalizedEmail =
      email.trim().toLowerCase()

    if (normalizedName.length < 2) {
      setError(
        "Introduza o seu nome.",
      )
      return
    }

    if (
      !normalizedEmail ||
      !normalizedEmail.includes("@")
    ) {
      setError(
        "Introduza um email válido.",
      )
      return
    }

    if (password.length < 8) {
      setError(
        "A palavra-passe deve ter pelo menos 8 caracteres.",
      )
      return
    }

    if (password !== confirmPassword) {
      setError(
        "As palavras-passe não coincidem.",
      )
      return
    }

    setLoading(true)
    setError("")

    try {
      await register({
        nome: normalizedName,
        email: normalizedEmail,
        password,
      })

      navigate("/")
    } catch (err) {
      if (
        axios.isAxiosError(err) &&
        err.response?.status === 409
      ) {
        setError(
          "Já existe uma conta com este email.",
        )
      } else {
        const apiMessage =
          axios.isAxiosError(err)
            ? err.response?.data?.message
            : null

        setError(
          apiMessage ??
            "Não foi possível criar a conta.",
        )
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page register-layout">
      <section className="auth-panel">
        <div className="auth-brand">
          <div className="brand-symbol">
            <Database size={25} />
          </div>

          <div className="brand-copy">
            <strong>
              Data<span>Forge</span>
            </strong>

            <small>
              Data Analytics Workspace
            </small>
          </div>
        </div>

        <div className="auth-container register-container">
          <Link
            to="/login"
            className="back-login"
          >
            <ArrowLeft size={16} />
            Voltar ao login
          </Link>

          <div className="auth-heading">
            <span className="auth-kicker">
              CRIAR CONTA
            </span>

            <h1>
              Comece no
              <span> DataForge.</span>
            </h1>

            <p>
              Crie a sua conta para organizar,
              validar e explorar datasets.
            </p>
          </div>

          <form
            className="auth-form"
            onSubmit={handleSubmit}
          >
            <label className="field">
              <span>Nome</span>

              <div className="input-wrapper">
                <User size={17} />

                <input
                  value={nome}
                  onChange={(event) => {
                    setNome(event.target.value)
                    setError("")
                  }}
                  placeholder="O seu nome"
                  autoComplete="name"
                  disabled={loading}
                />
              </div>
            </label>

            <label className="field">
              <span>Email</span>

              <div className="input-wrapper">
                <Mail size={17} />

                <input
                  type="email"
                  value={email}
                  onChange={(event) => {
                    setEmail(event.target.value)
                    setError("")
                  }}
                  placeholder="nome@empresa.com"
                  autoComplete="email"
                  disabled={loading}
                />
              </div>
            </label>

            <label className="field">
              <span>Palavra-passe</span>

              <div className="input-wrapper">
                <LockKeyhole size={17} />

                <input
                  type={
                    showPassword
                      ? "text"
                      : "password"
                  }
                  value={password}
                  onChange={(event) => {
                    setPassword(event.target.value)
                    setError("")
                  }}
                  placeholder="Mínimo de 8 caracteres"
                  autoComplete="new-password"
                  disabled={loading}
                />

                <button
                  type="button"
                  className="password-toggle"
                  onClick={() =>
                    setShowPassword(
                      (current) => !current,
                    )
                  }
                >
                  {showPassword ? (
                    <EyeOff size={17} />
                  ) : (
                    <Eye size={17} />
                  )}
                </button>
              </div>
            </label>

            <label className="field">
              <span>
                Confirmar palavra-passe
              </span>

              <div className="input-wrapper">
                <LockKeyhole size={17} />

                <input
                  type={
                    showPassword
                      ? "text"
                      : "password"
                  }
                  value={confirmPassword}
                  onChange={(event) => {
                    setConfirmPassword(
                      event.target.value,
                    )
                    setError("")
                  }}
                  placeholder="Repita a palavra-passe"
                  autoComplete="new-password"
                  disabled={loading}
                />
              </div>
            </label>

            {error && (
              <div
                className="auth-message"
                role="alert"
              >
                {error}
              </div>
            )}

            <button
              type="submit"
              className="auth-primary"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="button-spinner" />
                  A criar conta...
                </>
              ) : (
                <>
                  <UserPlus size={18} />
                  Criar conta
                  <ArrowRight size={18} />
                </>
              )}
            </button>
          </form>
        </div>
      </section>

      <section className="auth-showcase register-showcase">
        <div className="showcase-glow glow-one" />
        <div className="showcase-grid" />

        <div className="register-message">
          <div className="register-icon">
            <Database size={31} />
          </div>

          <span>
            DATAFORGE WORKSPACE
          </span>

          <h2>
            Um lugar para os seus
            <strong> dados evoluírem.</strong>
          </h2>

          <p>
            Organize projetos, importe datasets,
            encontre problemas de qualidade e
            explore informação com clareza.
          </p>
        </div>
      </section>
    </div>
  )
}
