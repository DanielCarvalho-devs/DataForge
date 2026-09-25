import {
  ArrowLeft,
  ArrowRight,
  Database,
  FileSpreadsheet,
  FolderKanban,
  LoaderCircle,
  RefreshCw,
  Upload,
  X,
  Server,
  FileUp,
  CheckCircle2,
  Eye,
} from "lucide-react"

import {
  useCallback,
  useEffect,
  useState,
} from "react"

import {
  Link,
  useNavigate,
  useParams,
} from "react-router-dom"

import { api } from "../api/client"

import {
  data,
  mensagemErro,
  normalizarLista,
  numero,
} from "../components/workspace/workspaceTypes"

import type {
  Dataset,
  Projeto,
} from "../components/workspace/workspaceTypes"

export function ProjectWorkspacePage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const idProjeto = Number(id)

  const [projeto, setProjeto] =
    useState<Projeto | null>(null)

  const [datasets, setDatasets] =
    useState<Dataset[]>([])

  const [loading, setLoading] =
    useState(true)

  const [erro, setErro] =
    useState<string | null>(null)

  const [importOpen, setImportOpen] = useState(false)
  const [importTab, setImportTab] =
    useState<"arquivo" | "sql">("arquivo")

  const [importLoading, setImportLoading] =
    useState(false)

  const [importMessage, setImportMessage] =
    useState<string | null>(null)

  const [arquivo, setArquivo] =
    useState<File | null>(null)

  const [nomeImport, setNomeImport] =
    useState("")

  const [sqlServer, setSqlServer] =
    useState("")

  const [sqlDatabase, setSqlDatabase] =
    useState("")

  const [sqlWindows, setSqlWindows] =
    useState(true)

  const [sqlUser, setSqlUser] =
    useState("")

  const [sqlPassword, setSqlPassword] =
    useState("")

  const [sqlObjetos, setSqlObjetos] =
    useState<Record<string, unknown>[]>([])

  const [sqlObjeto, setSqlObjeto] =
    useState("")

  const [sqlQuery, setSqlQuery] =
    useState("")

  const [sqlPreview, setSqlPreview] =
    useState<Record<string, unknown>[]>([])

  function sqlConnectionPayload() {
    return {
      server: sqlServer,
      database: sqlDatabase,
      useWindowsAuth: sqlWindows,
      username: sqlWindows ? null : sqlUser,
      password: sqlWindows ? null : sqlPassword,
    }
  }

  async function importarArquivo() {
    if (!arquivo) {
      setImportMessage("Selecione um ficheiro CSV ou XLSX.")
      return
    }

    try {
      setImportLoading(true)
      setImportMessage(null)

      const form = new FormData()
      form.append("idProjeto", String(idProjeto))
      form.append("arquivo", arquivo)

      if (nomeImport.trim()) {
        form.append("nome", nomeImport.trim())
      }

      const response = await api.post(
        "/data-transfer/arquivo",
        form,
        {
          headers: {
            "Content-Type": "multipart/form-data",
          },
        },
      )

      setImportMessage(
        response.data?.mensagem ??
          "Dataset importado com sucesso.",
      )

      setArquivo(null)
      setNomeImport("")

      await carregar()

      window.setTimeout(() => {
        setImportOpen(false)
        setImportMessage(null)
      }, 900)
    } catch (error) {
      setImportMessage(
        mensagemErro(
          error,
          "Nao foi possivel importar o ficheiro.",
        ),
      )
    } finally {
      setImportLoading(false)
    }
  }

  async function testarSql() {
    try {
      setImportLoading(true)
      setImportMessage(null)

      const response = await api.post(
        "/data-transfer/sql/testar",
        sqlConnectionPayload(),
      )

      setImportMessage(
        response.data?.mensagem ??
          "Ligacao estabelecida.",
      )

      const objetosResponse = await api.post(
        "/data-transfer/sql/objetos",
        sqlConnectionPayload(),
      )

      setSqlObjetos(
        normalizarLista<Record<string, unknown>>(
          objetosResponse.data,
        ),
      )
    } catch (error) {
      setImportMessage(
        mensagemErro(
          error,
          "Nao foi possivel ligar ao SQL Server.",
        ),
      )
    } finally {
      setImportLoading(false)
    }
  }

  function obterSqlSelection() {
    if (!sqlObjeto) {
      return {
        schema: null,
        tabela: null,
      }
    }

    const [schema, ...resto] =
      sqlObjeto.split(".")

    return {
      schema,
      tabela: resto.join("."),
    }
  }

  async function previewSql() {
    try {
      setImportLoading(true)
      setImportMessage(null)

      const selection = obterSqlSelection()

      const response = await api.post(
        "/data-transfer/sql/preview",
        {
          ...sqlConnectionPayload(),
          idProjeto,
          nomeDataset: nomeImport || null,
          schema: selection.schema,
          tabela: selection.tabela,
          query: sqlQuery.trim() || null,
        },
      )

      setSqlPreview(
        normalizarLista<Record<string, unknown>>(
          response.data?.registos,
        ),
      )

      setImportMessage("Preview SQL carregado.")
    } catch (error) {
      setImportMessage(
        mensagemErro(
          error,
          "Nao foi possivel gerar o preview SQL.",
        ),
      )
    } finally {
      setImportLoading(false)
    }
  }

  async function importarSql() {
    try {
      setImportLoading(true)
      setImportMessage(null)

      const selection = obterSqlSelection()

      const response = await api.post(
        "/data-transfer/sql/importar",
        {
          ...sqlConnectionPayload(),
          idProjeto,
          nomeDataset: nomeImport || null,
          schema: selection.schema,
          tabela: selection.tabela,
          query: sqlQuery.trim() || null,
        },
      )

      setImportMessage(
        response.data?.mensagem ??
          "SQL importado com sucesso.",
      )

      await carregar()

      window.setTimeout(() => {
        setImportOpen(false)
        setImportMessage(null)
        setSqlPreview([])
      }, 900)
    } catch (error) {
      setImportMessage(
        mensagemErro(
          error,
          "Nao foi possivel importar os dados SQL.",
        ),
      )
    } finally {
      setImportLoading(false)
    }
  }

  const carregar = useCallback(async () => {
    if (!Number.isFinite(idProjeto)) {
      setErro("Projeto invalido.")
      setLoading(false)
      return
    }

    try {
      setLoading(true)
      setErro(null)

      const [projetoResponse, datasetsResponse] =
        await Promise.all([
          api.get(`/projetos/${idProjeto}`),
          api.get(`/datasets/projeto/${idProjeto}`),
        ])

      setProjeto(projetoResponse.data)

      const lista =
        normalizarLista<Dataset>(datasetsResponse.data)

      lista.sort(
        (a, b) =>
          new Date(b.dataImportacao ?? 0).getTime() -
          new Date(a.dataImportacao ?? 0).getTime(),
      )

      setDatasets(lista)
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel carregar o projeto.",
        ),
      )
    } finally {
      setLoading(false)
    }
  }, [idProjeto])

  useEffect(() => {
    void carregar()
  }, [carregar])

  if (loading) {
    return (
      <div className="df-loading">
        <LoaderCircle className="spin" size={28} />
        <strong>A carregar projeto</strong>
      </div>
    )
  }

  if (erro || !projeto) {
    return (
      <div className="df-error-state">
        <FolderKanban size={32} />
        <h2>Projeto indisponivel</h2>
        <p>{erro ?? "Projeto nao encontrado."}</p>
        <Link to="/projetos">Voltar aos projetos</Link>
      </div>
    )
  }

  return (
    <div className="df-workspace-page">

      <header className="df-workspace-header">
        <div>
          <Link
            to="/projetos"
            className="df-back-link"
          >
            <ArrowLeft size={14} />
            Projetos
          </Link>

          <span className="df-kicker">
            PROJECT WORKSPACE
          </span>

          <h1>{projeto.nome}</h1>

          <p>
            {projeto.descricao ??
              "Workspace de dados do projeto."}
          </p>
        </div>

        <div className="df-header-actions">
          <button
            type="button"
            className="df-icon-button"
            onClick={() => void carregar()}
            title="Atualizar"
          >
            <RefreshCw size={17} />
          </button>

          <button
            type="button"
            className="df-primary-button"
            onClick={() => {
              setImportMessage(null)
              setImportOpen(true)
            }}
          >
            <Upload size={16} />
            Importar dados
          </button>
        </div>
      </header>

      <section className="df-project-kpis">
        <article>
          <FolderKanban size={19} />
          <span>Projeto</span>
          <strong>Ativo</strong>
        </article>

        <article>
          <Database size={19} />
          <span>Datasets</span>
          <strong>{numero(datasets.length)}</strong>
        </article>

        <article>
          <FileSpreadsheet size={19} />
          <span>Registos</span>
          <strong>
            {numero(
              datasets.reduce(
                (total, dataset) =>
                  total + (dataset.totalRegistos ?? 0),
                0,
              ),
            )}
          </strong>
        </article>
      </section>

      <section className="df-section">
        <div className="df-section-heading">
          <div>
            <span className="df-kicker">
              DATASETS
            </span>
            <h2>Dados do projeto</h2>
          </div>
        </div>

        {datasets.length === 0 ? (
          <div className="df-empty">
            <Database size={30} />
            <h3>Nenhum dataset</h3>
            <p>
              Este projeto ainda nao possui datasets.
            </p>
          </div>
        ) : (
          <div className="df-dataset-grid">
            {datasets.map((dataset) => (
              <article
                className="df-dataset-card"
                key={dataset.idDataset}
              >
                <div className="df-dataset-icon">
                  <FileSpreadsheet size={21} />
                </div>

                <div className="df-dataset-card-main">
                  <span className="df-file-type">
                    {dataset.tipoArquivo ?? "DATASET"}
                  </span>

                  <h3>{dataset.nome}</h3>

                  <div className="df-dataset-meta">
                    <span>
                      {numero(dataset.totalRegistos)} registos
                    </span>
                    <span>
                      {numero(dataset.totalColunas)} colunas
                    </span>
                    <span>
                      {data(dataset.dataImportacao)}
                    </span>
                  </div>
                </div>

                <button
                  type="button"
                  className="df-open-button"
                  onClick={() =>
                    navigate(
                      `/datasets/${dataset.idDataset}`,
                    )
                  }
                >
                  Abrir workspace
                  <ArrowRight size={15} />
                </button>
              </article>
            ))}
          </div>
        )}
      </section>

      {importOpen && (
        <div
          className="df-transfer-overlay"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setImportOpen(false)
            }
          }}
        >
          <section className="df-transfer-modal">
            <header className="df-transfer-header">
              <div>
                <span className="df-kicker">
                  DATA IMPORT
                </span>
                <h2>Importar dados</h2>
                <p>
                  Adicione CSV, Excel ou importe diretamente
                  de um SQL Server.
                </p>
              </div>

              <button
                type="button"
                className="df-icon-button"
                onClick={() => setImportOpen(false)}
              >
                <X size={18} />
              </button>
            </header>

            <div className="df-transfer-tabs">
              <button
                type="button"
                className={
                  importTab === "arquivo"
                    ? "active"
                    : ""
                }
                onClick={() => {
                  setImportTab("arquivo")
                  setImportMessage(null)
                }}
              >
                <FileUp size={16} />
                CSV / Excel
              </button>

              <button
                type="button"
                className={
                  importTab === "sql"
                    ? "active"
                    : ""
                }
                onClick={() => {
                  setImportTab("sql")
                  setImportMessage(null)
                }}
              >
                <Server size={16} />
                SQL Server
              </button>
            </div>

            <div className="df-transfer-body">
              <label className="df-field">
                <span>Nome do dataset</span>
                <input
                  value={nomeImport}
                  onChange={(event) =>
                    setNomeImport(event.target.value)
                  }
                  placeholder="Opcional"
                />
              </label>

              {importTab === "arquivo" ? (
                <>
                  <label className="df-file-drop">
                    <FileSpreadsheet size={32} />

                    <strong>
                      {arquivo
                        ? arquivo.name
                        : "Selecionar CSV ou Excel"}
                    </strong>

                    <span>
                      CSV ou XLSX · máximo 50 MB
                    </span>

                    <input
                      type="file"
                      accept=".csv,.xlsx"
                      onChange={(event) =>
                        setArquivo(
                          event.target.files?.[0] ?? null,
                        )
                      }
                    />
                  </label>

                  <button
                    type="button"
                    className="df-primary-button df-transfer-main-button"
                    disabled={importLoading || !arquivo}
                    onClick={() => void importarArquivo()}
                  >
                    {importLoading ? (
                      <LoaderCircle
                        size={16}
                        className="spin"
                      />
                    ) : (
                      <Upload size={16} />
                    )}

                    {importLoading
                      ? "A importar..."
                      : "Importar ficheiro"}
                  </button>
                </>
              ) : (
                <>
                  <div className="df-sql-grid">
                    <label className="df-field">
                      <span>Servidor</span>
                      <input
                        value={sqlServer}
                        onChange={(event) =>
                          setSqlServer(event.target.value)
                        }
                        placeholder="SERVIDOR\SQLEXPRESS"
                      />
                    </label>

                    <label className="df-field">
                      <span>Base de dados</span>
                      <input
                        value={sqlDatabase}
                        onChange={(event) =>
                          setSqlDatabase(event.target.value)
                        }
                        placeholder="NomeDaBase"
                      />
                    </label>
                  </div>

                  <label className="df-check-row">
                    <input
                      type="checkbox"
                      checked={sqlWindows}
                      onChange={(event) =>
                        setSqlWindows(event.target.checked)
                      }
                    />
                    <span>
                      Usar Windows Authentication
                    </span>
                  </label>

                  {!sqlWindows && (
                    <div className="df-sql-grid">
                      <label className="df-field">
                        <span>Utilizador SQL</span>
                        <input
                          value={sqlUser}
                          onChange={(event) =>
                            setSqlUser(event.target.value)
                          }
                        />
                      </label>

                      <label className="df-field">
                        <span>Password</span>
                        <input
                          type="password"
                          value={sqlPassword}
                          onChange={(event) =>
                            setSqlPassword(event.target.value)
                          }
                        />
                      </label>
                    </div>
                  )}

                  <button
                    type="button"
                    className="df-secondary-button"
                    disabled={
                      importLoading ||
                      !sqlServer.trim() ||
                      !sqlDatabase.trim()
                    }
                    onClick={() => void testarSql()}
                  >
                    <CheckCircle2 size={16} />
                    Testar ligação e carregar tabelas
                  </button>

                  {sqlObjetos.length > 0 && (
                    <label className="df-field">
                      <span>Tabela ou view</span>

                      <select
                        value={sqlObjeto}
                        onChange={(event) =>
                          setSqlObjeto(event.target.value)
                        }
                      >
                        <option value="">
                          Selecione...
                        </option>

                        {sqlObjetos.map((item, index) => {
                          const schema =
                            String(item.schema ?? "dbo")

                          const nome =
                            String(item.nome ?? "")

                          const tipo =
                            String(item.tipo ?? "")

                          return (
                            <option
                              key={`${schema}.${nome}.${index}`}
                              value={`${schema}.${nome}`}
                            >
                              {schema}.{nome} · {tipo}
                            </option>
                          )
                        })}
                      </select>
                    </label>
                  )}

                  <div className="df-sql-divider">
                    <span>OU CONSULTA PERSONALIZADA</span>
                  </div>

                  <label className="df-field">
                    <span>
                      SELECT somente leitura
                    </span>

                    <textarea
                      rows={5}
                      value={sqlQuery}
                      onChange={(event) =>
                        setSqlQuery(event.target.value)
                      }
                      placeholder="SELECT Coluna1, Coluna2 FROM dbo.Tabela"
                    />
                  </label>

                  <div className="df-transfer-actions">
                    <button
                      type="button"
                      className="df-secondary-button"
                      disabled={
                        importLoading ||
                        (!sqlObjeto && !sqlQuery.trim())
                      }
                      onClick={() => void previewSql()}
                    >
                      <Eye size={16} />
                      Preview
                    </button>

                    <button
                      type="button"
                      className="df-primary-button"
                      disabled={
                        importLoading ||
                        (!sqlObjeto && !sqlQuery.trim())
                      }
                      onClick={() => void importarSql()}
                    >
                      {importLoading ? (
                        <LoaderCircle
                          size={16}
                          className="spin"
                        />
                      ) : (
                        <Upload size={16} />
                      )}

                      Importar SQL
                    </button>
                  </div>

                  {sqlPreview.length > 0 && (
                    <div className="df-sql-preview">
                      <div className="df-table-scroll">
                        <table className="df-table">
                          <thead>
                            <tr>
                              {Object.keys(
                                sqlPreview[0],
                              ).map((coluna) => (
                                <th key={coluna}>
                                  {coluna}
                                </th>
                              ))}
                            </tr>
                          </thead>

                          <tbody>
                            {sqlPreview.map(
                              (linha, index) => (
                                <tr key={index}>
                                  {Object.keys(
                                    sqlPreview[0],
                                  ).map((coluna) => (
                                    <td key={coluna}>
                                      {String(
                                        linha[coluna] ?? "",
                                      )}
                                    </td>
                                  ))}
                                </tr>
                              ),
                            )}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}
                </>
              )}

              {importMessage && (
                <div className="df-transfer-message">
                  {importMessage}
                </div>
              )}
            </div>
          </section>
        </div>
      )}
    </div>
  )
}


