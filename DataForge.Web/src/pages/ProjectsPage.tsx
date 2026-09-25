import {
  AlertTriangle,
  CheckCircle2,
  Database,
  Edit3,
  FolderKanban,
  LoaderCircle,
  MoreHorizontal,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  X,
} from "lucide-react"

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react"

import type {
  FormEvent,
} from "react"

import { useNavigate } from "react-router-dom"

import { api } from "../api/client"

type Projeto = {
  idProjeto: number
  idUtilizador: number
  nome: string
  descricao?: string | null
  ativo: boolean
  dataCriacao: string
  dataAtualizacao?: string | null
}

type Dataset = {
  idDataset: number
  idProjeto: number
  nome: string
  estado?: string
  dataImportacao?: string
}

type ProjetoWorkspace = Projeto & {
  totalDatasets: number
}

function normalizarLista<T>(
  valor: unknown,
): T[] {
  if (Array.isArray(valor)) {
    return valor as T[]
  }

  if (
    valor &&
    typeof valor === "object"
  ) {
    const objeto =
      valor as Record<string, unknown>

    const candidatos = [
      objeto.items,
      objeto.data,
      objeto.resultados,
      objeto.projetos,
      objeto.datasets,
    ]

    for (
      const candidato
      of candidatos
    ) {
      if (Array.isArray(candidato)) {
        return candidato as T[]
      }
    }
  }

  return []
}

function mensagemErro(
  erro: unknown,
  fallback: string,
) {
  if (
    erro &&
    typeof erro === "object" &&
    "response" in erro
  ) {
    const response = (
      erro as {
        response?: {
          data?: {
            mensagem?: string
          }
        }
      }
    ).response

    if (response?.data?.mensagem) {
      return response.data.mensagem
    }
  }

  return fallback
}

function formatarData(
  valor?: string | null,
) {
  if (!valor) {
    return "—"
  }

  const data = new Date(valor)

  if (
    Number.isNaN(
      data.getTime(),
    )
  ) {
    return "—"
  }

  return new Intl.DateTimeFormat(
    "pt-PT",
    {
      day: "2-digit",
      month: "short",
      year: "numeric",
    },
  ).format(data)
}

export function ProjectsPage() {
  const navigate = useNavigate()
  const [
    projetos,
    setProjetos,
  ] = useState<ProjetoWorkspace[]>([])

  const [
    loading,
    setLoading,
  ] = useState(true)

  const [
    refreshing,
    setRefreshing,
  ] = useState(false)

  const [
    saving,
    setSaving,
  ] = useState(false)

  const [
    deletingId,
    setDeletingId,
  ] = useState<number | null>(null)

  const [
    pesquisa,
    setPesquisa,
  ] = useState("")

  const [
    erro,
    setErro,
  ] = useState<string | null>(null)

  const [
    sucesso,
    setSucesso,
  ] = useState<string | null>(null)

  const [
    modalAberto,
    setModalAberto,
  ] = useState(false)

  const [
    projetoEditando,
    setProjetoEditando,
  ] = useState<ProjetoWorkspace | null>(
    null,
  )

  const [
    nome,
    setNome,
  ] = useState("")

  const [
    descricao,
    setDescricao,
  ] = useState("")

  const [
    menuAberto,
    setMenuAberto,
  ] = useState<number | null>(null)

  const carregarProjetos =
    useCallback(
      async (
        atualizacao = false,
      ) => {
        try {
          if (atualizacao) {
            setRefreshing(true)
          } else {
            setLoading(true)
          }

          setErro(null)

          const response =
            await api.get(
              "/projetos",
            )

          const lista =
            normalizarLista<Projeto>(
              response.data,
            ).filter(
              (projeto) =>
                projeto.ativo !==
                false,
            )

          const completos =
            await Promise.all(
              lista.map(
                async (
                  projeto,
                ): Promise<ProjetoWorkspace> => {
                  try {
                    const responseDatasets =
                      await api.get(
                        `/datasets/projeto/${projeto.idProjeto}`,
                      )

                    const datasets =
                      normalizarLista<Dataset>(
                        responseDatasets.data,
                      )

                    return {
                      ...projeto,
                      totalDatasets:
                        datasets.length,
                    }
                  } catch {
                    return {
                      ...projeto,
                      totalDatasets: 0,
                    }
                  }
                },
              ),
            )

          completos.sort(
            (a, b) => {
              const dataA =
                new Date(
                  a.dataAtualizacao ??
                    a.dataCriacao,
                ).getTime()

              const dataB =
                new Date(
                  b.dataAtualizacao ??
                    b.dataCriacao,
                ).getTime()

              return dataB - dataA
            },
          )

          setProjetos(
            completos,
          )
        } catch (error) {
          console.error(error)

          setErro(
            mensagemErro(
              error,
              "Nao foi possivel carregar os projetos.",
            ),
          )
        } finally {
          setLoading(false)
          setRefreshing(false)
        }
      },
      [],
    )

  useEffect(() => {
    void carregarProjetos()
  }, [carregarProjetos])

  const projetosFiltrados =
    useMemo(() => {
      const termo =
        pesquisa
          .trim()
          .toLowerCase()

      if (!termo) {
        return projetos
      }

      return projetos.filter(
        (projeto) =>
          projeto.nome
            .toLowerCase()
            .includes(termo) ||
          (
            projeto.descricao ??
            ""
          )
            .toLowerCase()
            .includes(termo),
      )
    }, [
      projetos,
      pesquisa,
    ])

  const totalDatasets =
    useMemo(
      () =>
        projetos.reduce(
          (
            total,
            projeto,
          ) =>
            total +
            projeto.totalDatasets,
          0,
        ),
      [projetos],
    )

  function abrirNovoProjeto() {
    setProjetoEditando(null)
    setNome("")
    setDescricao("")
    setErro(null)
    setMenuAberto(null)
    setModalAberto(true)
  }

  function abrirEdicao(
    projeto: ProjetoWorkspace,
  ) {
    setProjetoEditando(
      projeto,
    )

    setNome(projeto.nome)

    setDescricao(
      projeto.descricao ??
        "",
    )

    setErro(null)
    setMenuAberto(null)
    setModalAberto(true)
  }

  function fecharModal() {
    if (saving) {
      return
    }

    setModalAberto(false)
    setProjetoEditando(null)
    setNome("")
    setDescricao("")
  }

  async function guardar(
    event:
      FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    const nomeFinal =
      nome.trim()

    const descricaoFinal =
      descricao.trim()

    if (!nomeFinal) {
      setErro(
        "O nome do projeto e obrigatorio.",
      )
      return
    }

    try {
      setSaving(true)
      setErro(null)

      if (projetoEditando) {
        await api.put(
          `/projetos/${projetoEditando.idProjeto}`,
          {
            nome: nomeFinal,
            descricao:
              descricaoFinal ||
              null,
            ativo: true,
          },
        )

        setSucesso(
          "Projeto atualizado com sucesso.",
        )
      } else {
        await api.post(
          "/projetos",
          {
            nome: nomeFinal,
            descricao:
              descricaoFinal ||
              null,
          },
        )

        setSucesso(
          "Projeto criado com sucesso.",
        )
      }

      fecharModal()

      await carregarProjetos(
        true,
      )
    } catch (error) {
      console.error(error)

      setErro(
        mensagemErro(
          error,
          "Nao foi possivel guardar o projeto.",
        ),
      )
    } finally {
      setSaving(false)
    }
  }

  async function desativar(
    projeto: ProjetoWorkspace,
  ) {
    setMenuAberto(null)

    const confirmar =
      window.confirm(
        `Desativar o projeto "${projeto.nome}"?`,
      )

    if (!confirmar) {
      return
    }

    try {
      setDeletingId(
        projeto.idProjeto,
      )

      setErro(null)

      await api.delete(
        `/projetos/${projeto.idProjeto}`,
      )

      setProjetos(
        (atuais) =>
          atuais.filter(
            (item) =>
              item.idProjeto !==
              projeto.idProjeto,
          ),
      )

      setSucesso(
        "Projeto desativado com sucesso.",
      )
    } catch (error) {
      console.error(error)

      setErro(
        mensagemErro(
          error,
          "Nao foi possivel desativar o projeto.",
        ),
      )
    } finally {
      setDeletingId(null)
    }
  }

  if (loading) {
    return (
      <div className="page">
        <section className="empty-state">
          <LoaderCircle
            size={28}
            className="spin"
          />

          <h2>
            A carregar projetos
          </h2>

          <p>
            A consultar o DataForge...
          </p>
        </section>
      </div>
    )
  }

  return (
    <div className="page projects-functional">

      <header className="page-header horizontal">

        <div>
          <span className="eyebrow">
            WORKSPACE
          </span>

          <h1>
            Projetos
          </h1>

          <p>
            Organize seus datasets
            por projeto.
          </p>
        </div>

        <div className="projects-header-buttons">

          <button
            type="button"
            className="projects-refresh-button"
            disabled={refreshing}
            onClick={() =>
              void carregarProjetos(
                true,
              )
            }
          >
            <RefreshCw
              size={17}
              className={
                refreshing
                  ? "spin"
                  : ""
              }
            />
          </button>

          <button
            type="button"
            className="primary-button compact"
            onClick={
              abrirNovoProjeto
            }
          >
            <Plus size={18} />
            Novo projeto
          </button>

        </div>

      </header>

      {erro && (
        <div className="projects-feedback projects-feedback-error">
          <AlertTriangle
            size={17}
          />

          <span>{erro}</span>

          <button
            type="button"
            onClick={() =>
              setErro(null)
            }
          >
            <X size={15} />
          </button>
        </div>
      )}

      {sucesso && (
        <div className="projects-feedback projects-feedback-success">
          <CheckCircle2
            size={17}
          />

          <span>{sucesso}</span>

          <button
            type="button"
            onClick={() =>
              setSucesso(null)
            }
          >
            <X size={15} />
          </button>
        </div>
      )}

      <section className="projects-simple-stats">

        <div>
          <FolderKanban
            size={20}
          />

          <span>
            Projetos ativos
          </span>

          <strong>
            {projetos.length}
          </strong>
        </div>

        <div>
          <Database
            size={20}
          />

          <span>
            Datasets
          </span>

          <strong>
            {totalDatasets}
          </strong>
        </div>

      </section>

      <section className="projects-list-section">

        <div className="projects-list-heading">

          <div>
            <span className="eyebrow">
              SEUS PROJETOS
            </span>

            <h2>
              Workspaces de dados
            </h2>
          </div>

          <label className="projects-search-box">

            <Search size={16} />

            <input
              type="text"
              placeholder="Pesquisar projetos..."
              value={pesquisa}
              onChange={(
                event,
              ) =>
                setPesquisa(
                  event.target
                    .value,
                )
              }
            />

          </label>

        </div>

        {projetosFiltrados.length ===
        0 ? (
          <section className="empty-state">

            <FolderKanban
              size={28}
            />

            <h2>
              Nenhum projeto
            </h2>

            <p>
              Crie um projeto para
              organizar seus datasets.
            </p>

          </section>
        ) : (
          <div className="projects-functional-grid">

            {projetosFiltrados.map(
              (projeto) => (
                <article
                  className="project-functional-card"
                  key={
                    projeto.idProjeto
                  }
                >

                  <div className="project-card-header">

                    <div className="project-card-icon">
                      <FolderKanban
                        size={20}
                      />
                    </div>

                    <div className="project-card-menu-wrapper">

                      <button
                        type="button"
                        className="project-card-menu-button"
                        onClick={() =>
                          setMenuAberto(
                            menuAberto ===
                              projeto.idProjeto
                              ? null
                              : projeto.idProjeto,
                          )
                        }
                      >
                        <MoreHorizontal
                          size={18}
                        />
                      </button>

                      {menuAberto ===
                        projeto.idProjeto && (
                        <div className="project-card-menu">

                          <button
                            type="button"
                            onClick={() =>
                              abrirEdicao(
                                projeto,
                              )
                            }
                          >
                            <Edit3
                              size={14}
                            />
                            Editar
                          </button>

                          <button
                            type="button"
                            className="danger"
                            disabled={
                              deletingId ===
                              projeto.idProjeto
                            }
                            onClick={() =>
                              void desativar(
                                projeto,
                              )
                            }
                          >
                            {deletingId ===
                            projeto.idProjeto ? (
                              <LoaderCircle
                                size={14}
                                className="spin"
                              />
                            ) : (
                              <Trash2
                                size={14}
                              />
                            )}

                            Desativar
                          </button>

                        </div>
                      )}

                    </div>

                  </div>

                  <h3>
                    {projeto.nome}
                  </h3>

                  <p>
                    {projeto.descricao ||
                      "Projeto de analise de dados."}
                  </p>

                  <div className="project-card-info">

                    <span>
                      <Database
                        size={14}
                      />

                      {
                        projeto.totalDatasets
                      }{" "}
                      {projeto.totalDatasets ===
                      1
                        ? "dataset"
                        : "datasets"}
                    </span>

                    <span>
                      {formatarData(
                        projeto.dataAtualizacao ??
                          projeto.dataCriacao,
                      )}
                    </span>

                  </div>

                  <button
                    type="button"
                    className="project-workspace-open"
                    onClick={() =>
                      navigate(
                        `/projetos/${projeto.idProjeto}`,
                      )
                    }
                  >
                    Abrir projeto
                    <span>→</span>
                  </button>

                </article>
              ),
            )}

          </div>
        )}

      </section>

      {modalAberto && (
        <div
          className="project-simple-modal-backdrop"
          onMouseDown={(
            event,
          ) => {
            if (
              event.target ===
              event.currentTarget
            ) {
              fecharModal()
            }
          }}
        >

          <div className="project-simple-modal">

            <div className="project-simple-modal-header">

              <div>
                <span className="eyebrow">
                  {projetoEditando
                    ? "EDITAR PROJETO"
                    : "NOVO PROJETO"}
                </span>

                <h2>
                  {projetoEditando
                    ? "Editar projeto"
                    : "Criar projeto"}
                </h2>
              </div>

              <button
                type="button"
                onClick={
                  fecharModal
                }
              >
                <X size={18} />
              </button>

            </div>

            <form
              onSubmit={guardar}
              className="project-simple-form"
            >

              <label>
                Nome

                <input
                  type="text"
                  value={nome}
                  maxLength={150}
                  autoFocus
                  onChange={(
                    event,
                  ) =>
                    setNome(
                      event.target
                        .value,
                    )
                  }
                  placeholder="Nome do projeto"
                />
              </label>

              <label>
                Descricao

                <textarea
                  value={descricao}
                  maxLength={1000}
                  rows={5}
                  onChange={(
                    event,
                  ) =>
                    setDescricao(
                      event.target
                        .value,
                    )
                  }
                  placeholder="Objetivo do projeto..."
                />
              </label>

              <div className="project-simple-form-actions">

                <button
                  type="button"
                  className="secondary-button"
                  onClick={
                    fecharModal
                  }
                  disabled={saving}
                >
                  Cancelar
                </button>

                <button
                  type="submit"
                  className="primary-button compact"
                  disabled={
                    saving ||
                    !nome.trim()
                  }
                >
                  {saving && (
                    <LoaderCircle
                      size={15}
                      className="spin"
                    />
                  )}

                  {saving
                    ? "A guardar..."
                    : projetoEditando
                      ? "Guardar"
                      : "Criar projeto"}
                </button>

              </div>

            </form>

          </div>

        </div>
      )}

    </div>
  )
}


