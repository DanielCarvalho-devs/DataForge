import axios from "axios"

import {
  ArrowRight,
  BarChart3,
  Check,
  Database,
  Eye,
  EyeOff,
  LockKeyhole,
  Mail,
  ShieldCheck,
  Sparkles,
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

export function LoginPage() {
  const { login, authenticated } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [showPassword, setShowPassword] =
    useState(false)

  const [keepSession, setKeepSession] =
    useState(true)

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState("")

  if (authenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    const normalizedEmail =
      email.trim().toLowerCase()

    if (!normalizedEmail) {
      setError("Introduza o seu email.")
      return
    }

    if (!normalizedEmail.includes("@")) {
      setError("Introduza um email válido.")
      return
    }

    if (!password) {
      setError("Introduza a sua palavra-passe.")
      return
    }

    setLoading(true)
    setError("")

    try {
      await login(
        {
          email: normalizedEmail,
          password,
        },
        keepSession,
      )

      navigate("/")
    } catch (err) {
      if (
        axios.isAxiosError(err) &&
        err.response?.status === 401
      ) {
        setError(
          "Email ou palavra-passe incorretos.",
        )
      } else {
        setError(
          "Não foi possível ligar ao DataForge. Tente novamente.",
        )
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
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

        <div className="auth-container">
          <div className="auth-heading">
            <span className="auth-kicker">
              BEM-VINDO AO DATAFORGE
            </span>

            <h1>
              A sua plataforma de
              <span> análise de dados.</span>
            </h1>

            <p>
              Importe, valide, explore e transforme
              os seus datasets num único ambiente,
              com segurança e simplicidade.
            </p>
          </div>

          <form
            className="auth-form"
            onSubmit={handleSubmit}
          >
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
                  placeholder="A sua palavra-passe"
                  autoComplete="current-password"
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
                  aria-label={
                    showPassword
                      ? "Ocultar palavra-passe"
                      : "Mostrar palavra-passe"
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

            <div className="auth-options">
              <label className="remember-option">
                <input
                  type="checkbox"
                  checked={keepSession}
                  onChange={(event) =>
                    setKeepSession(
                      event.target.checked,
                    )
                  }
                />

                <span className="custom-check">
                  {keepSession && (
                    <Check size={12} />
                  )}
                </span>

                Manter sessão
              </label>

              <button
                type="button"
                className="forgot-button"
                title="Recuperação de palavra-passe será adicionada numa próxima versão."
                onClick={() => {
                  setError(
                    "A recuperação de palavra-passe será disponibilizada em breve.",
                  )
                }}
              >
                Esqueci a palavra-passe
              </button>
            </div>

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
                  A entrar...
                </>
              ) : (
                <>
                  Entrar no DataForge
                  <ArrowRight size={18} />
                </>
              )}
            </button>

            <div className="auth-divider">
              <span />
              <small>ou</small>
              <span />
            </div>

            <Link
              to="/registar"
              className="create-account-button"
            >
              <UserPlus size={20} />

              <span>
                <strong>Criar uma conta</strong>
                <small>
                  Comece a utilizar o DataForge
                </small>
              </span>

              <ArrowRight size={17} />
            </Link>
          </form>

          <div className="security-note">
            <ShieldCheck size={18} />

            <div>
              <strong>Ambiente seguro</strong>
              <span>
                Acesso protegido por autenticação.
              </span>
            </div>
          </div>
        </div>
      </section>

      <section className="auth-showcase">
        <div className="showcase-glow glow-one" />
        <div className="showcase-glow glow-two" />
        <div className="showcase-grid" />

        <div className="showcase-content">
          <div className="showcase-badge">
            <Sparkles size={15} />
            DATA WORKSPACE
          </div>

          <h2>
            Dados confiáveis.
            <span> Decisões melhores.</span>
          </h2>

          <p>
            Um workspace criado para transformar
            dados brutos em informação clara,
            estruturada e pronta para análise.
          </p>

          <div className="showcase-window">
            <div className="window-header">
              <div className="window-title">
                <Database size={18} />

                <div>
                  <strong>
                    Workspace de análise
                  </strong>
                  <span>
                    Do ficheiro ao insight
                  </span>
                </div>
              </div>

              <div className="window-status">
                <span />
                Pronto
              </div>
            </div>

            <div className="flow-line">
              <div className="flow-step active">
                <div>
                  <Database size={18} />
                </div>
                <span>Importar</span>
              </div>

              <i />

              <div className="flow-step">
                <div>
                  <ShieldCheck size={18} />
                </div>
                <span>Validar</span>
              </div>

              <i />

              <div className="flow-step">
                <div>
                  <BarChart3 size={18} />
                </div>
                <span>Explorar</span>
              </div>

              <i />

              <div className="flow-step">
                <div>
                  <Sparkles size={18} />
                </div>
                <span>Analisar</span>
              </div>
            </div>

            <div className="data-lines">
              <span className="line line-1" />
              <span className="line line-2" />
              <span className="line line-3" />
              <span className="line line-4" />
              <span className="line line-5" />
            </div>
          </div>

          <div className="showcase-features">
            <div>
              <ShieldCheck size={18} />
              <span>
                <strong>Qualidade</strong>
                <small>
                  Identifique problemas nos dados
                </small>
              </span>
            </div>

            <div>
              <BarChart3 size={18} />
              <span>
                <strong>Exploração</strong>
                <small>
                  Entenda os seus datasets
                </small>
              </span>
            </div>

            <div>
              <Sparkles size={18} />
              <span>
                <strong>Transformação</strong>
                <small>
                  Prepare dados para análise
                </small>
              </span>
            </div>
          </div>
        </div>

        <div className="showcase-footer">
          <span>DataForge</span>
          <i />
          <span>
            Data Analytics Workspace
          </span>
        </div>
      </section>
    </div>
  )
}
