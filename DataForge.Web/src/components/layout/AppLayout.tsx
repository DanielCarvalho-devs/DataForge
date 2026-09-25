import {
  ChevronDown,
  Database,
  FolderKanban,
  Home,
  KeyRound,
  LogOut,
  UserRound,
} from "lucide-react"

import {
  useEffect,
  useRef,
  useState,
} from "react"

import {
  NavLink,
  Outlet,
  useNavigate,
} from "react-router-dom"

import { useAuth } from "../../auth/AuthContext"

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

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const [menuOpen, setMenuOpen] =
    useState(false)

  const menuRef =
    useRef<HTMLDivElement | null>(null)

  const nome =
    user?.nome ?? "Daniel Carvalho"

  const perfil =
    user?.perfil ?? "Analista"

  const inicial =
    nome.trim().charAt(0).toUpperCase() || "D"

  const foto =
    fotoUrl(user?.fotoPerfil)

  useEffect(() => {
    function fechar(event: MouseEvent) {
      if (
        menuRef.current &&
        !menuRef.current.contains(
          event.target as Node,
        )
      ) {
        setMenuOpen(false)
      }
    }

    document.addEventListener(
      "mousedown",
      fechar,
    )

    return () =>
      document.removeEventListener(
        "mousedown",
        fechar,
      )
  }, [])

  function handleLogout() {
    logout()
    navigate("/login")
  }

  function irPerfil(
    section?: string,
  ) {
    setMenuOpen(false)
    navigate("/perfil")

    if (section) {
      window.setTimeout(() => {
        document
          .getElementById(section)
          ?.scrollIntoView({
            behavior: "smooth",
          })
      }, 80)
    }
  }

  return (
    <div className="df-top-shell">

      <header className="df-top-navbar">

        <div className="df-navbar-inner">

          <div className="df-navbar-left">

            <NavLink
              to="/"
              className="df-navbar-brand"
              aria-label="DataForge"
            >
              <span className="df-navbar-brand-icon">
                <Database size={20} />
              </span>

              <span className="df-navbar-brand-text">
                DataForge
              </span>
            </NavLink>

            <div className="df-navbar-divider" />

            <nav className="df-navbar-links">

              <NavLink
                to="/"
                end
                className={({ isActive }) =>
                  `df-navbar-link ${
                    isActive ? "active" : ""
                  }`
                }
              >
                <Home size={16} />
                <span>Início</span>
              </NavLink>

              <NavLink
                to="/projetos"
                className={({ isActive }) =>
                  `df-navbar-link ${
                    isActive ? "active" : ""
                  }`
                }
              >
                <FolderKanban size={16} />
                <span>Projetos</span>
              </NavLink>

            </nav>

          </div>

          <div
            className="df-account-menu-wrapper"
            ref={menuRef}
          >

            <button
              type="button"
              className="df-navbar-account-button"
              onClick={() =>
                setMenuOpen((value) => !value)
              }
              aria-expanded={menuOpen}
            >

              <div className="df-navbar-avatar">

                {foto ? (
                  <img
                    src={foto}
                    alt={nome}
                  />
                ) : (
                  inicial
                )}

              </div>

              <div className="df-navbar-user-info">
                <strong>{nome}</strong>
                <span>{perfil}</span>
              </div>

              <ChevronDown
                size={15}
                className={
                  menuOpen
                    ? "df-menu-chevron open"
                    : "df-menu-chevron"
                }
              />

            </button>

            {menuOpen && (
              <div className="df-account-dropdown">

                <div className="df-account-dropdown-header">

                  <div className="df-account-dropdown-avatar">
                    {foto ? (
                      <img
                        src={foto}
                        alt={nome}
                      />
                    ) : (
                      inicial
                    )}
                  </div>

                  <div>
                    <strong>{nome}</strong>
                    <span>{user?.email}</span>
                  </div>

                </div>

                <div className="df-account-menu-divider" />

                <button
                  type="button"
                  onClick={() =>
                    irPerfil()
                  }
                >
                  <UserRound size={16} />

                  <span>
                    <strong>Meu perfil</strong>
                    <small>
                      Dados pessoais e fotografia
                    </small>
                  </span>
                </button>

                <button
                  type="button"
                  onClick={() =>
                    irPerfil("seguranca")
                  }
                >
                  <KeyRound size={16} />

                  <span>
                    <strong>
                      Alterar password
                    </strong>

                    <small>
                      Segurança da conta
                    </small>
                  </span>
                </button>

                <div className="df-account-menu-divider" />

                <button
                  type="button"
                  className="df-account-logout"
                  onClick={handleLogout}
                >
                  <LogOut size={16} />

                  <span>
                    <strong>Sair</strong>
                    <small>
                      Terminar sessão
                    </small>
                  </span>
                </button>

              </div>
            )}

          </div>

        </div>

      </header>

      <main className="df-top-content">
        <Outlet />
      </main>

    </div>
  )
}
