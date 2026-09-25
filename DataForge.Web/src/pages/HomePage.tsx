import {
  AlertTriangle,
  ArrowRight,
  CheckCircle2,
  Database,
  FileSpreadsheet,
  FolderKanban,
  LoaderCircle,
  Plus,
  RefreshCw,
  Search,
  ShieldCheck,
  Sparkles,
  Upload,
} from "lucide-react"

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react"

import {
  Link,
  useNavigate,
} from "react-router-dom"

import { api } from "../api/client"
import { useAuth } from "../auth/AuthContext"

type Projeto = {
  idProjeto: number
  idUtilizador: number
  nome: string
  descricao?: string | null
  ativo: boolean
  dataCriacao?: string
  dataAtualizacao?: string | null
}

type Dataset = {
  idDataset: number
  idProjeto: number
  nome: string
  nomeOriginal: string
  tipoArquivo: string
  tamanhoBytes?: number | null
  totalRegistos: number
  totalColunas: number
  qualityScore?: number | null
  estado: string
  dataImportacao: string
  dataProcessamento?: string | null
}

type QualidadeExploracao = {
  totalProblemas: number
  problemasCriticos: number
  problemasErro: number
  problemasAviso: number
  problemasInformacao: number
  totalDuplicados: number
  totalOutliers: number
  totalValoresAusentes: number
}

type ExploracaoDataset = {
  idDataset: number
  nome: string
  nomeOriginal: string
  tipoArquivo: string
  estado: string
  totalRegistos: number
  totalColunas: number
  tamanhoBytes?: number | null
  qualityScore?: number | null
  dataImportacao: string
  dataProcessamento?: string | null
  qualidade: QualidadeExploracao
}

type DatasetWorkspace = Dataset & {
  projetoNome: string
}

function normalizarLista<T>(value: unknown): T[] {
  if (Array.isArray(value)) {
    return value as T[]
  }

  if (
    value &&
    typeof value === "object"
  ) {
    const objeto = value as Record<string, unknown>

    const candidatos = [
      objeto.items,
      objeto.data,
      objeto.resultados,
      objeto.projetos,
      objeto.datasets,
    ]

    const lista = candidatos.find(Array.isArray)

    if (Array.isArray(lista)) {
      return lista as T[]
    }
  }

  return []
}

function formatarNumero(value?: number | null) {
  return new Intl.NumberFormat("pt-PT").format(value ?? 0)
}

function formatarPercentual(value?: number | null) {
  if (
    value === null ||
    value === undefined
  ) {
    return "—"
  }

  return `${new Intl.NumberFormat("pt-PT", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value)}%`
}

function formatarTamanho(bytes?: number | null) {
  if (!bytes) {
    return "—"
  }

  if (bytes < 1024) {
    return `${bytes} B`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }

  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function formatarData(value?: string | null) {
  if (!value) {
    return "—"
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return "—"
  }

  return new Intl.DateTimeFormat("pt-PT", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(date)
}

function estadoClasse(estado: string) {
  const normalized = estado
    .trim()
    .toLocaleLowerCase("pt-PT")

  if (normalized === "processado") {
    return "success"
  }

  if (
    normalized === "a processar" ||
    normalized === "pendente"
  ) {
    return "processing"
  }

  if (normalized === "erro") {
    return "error"
  }

  return "neutral"
}

export function HomePage() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const [projetos, setProjetos] = useState<Projeto[]>([])
  const [datasets, setDatasets] = useState<DatasetWorkspace[]>([])
  const [exploracao, setExploracao] =
    useState<ExploracaoDataset | null>(null)

  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [pesquisa, setPesquisa] = useState("")

  const carregarWorkspace = useCallback(
    async (silencioso = false) => {
      try {
        if (silencioso) {
          setRefreshing(true)
        } else {
          setLoading(true)
        }

        setError(null)

        const projetosResponse =
          await api.get("/projetos")

        const projetosRecebidos =
          normalizarLista<Projeto>(
            projetosResponse.data,
          ).filter((projeto) => projeto.ativo !== false)

        setProjetos(projetosRecebidos)

        const respostasDatasets =
          await Promise.allSettled(
            projetosRecebidos.map(
              async (projeto) => {
                const response = await api.get(
                  `/datasets/projeto/${projeto.idProjeto}`,
                )

                return normalizarLista<Dataset>(
                  response.data,
                ).map(
                  (dataset): DatasetWorkspace => ({
                    ...dataset,
                    projetoNome: projeto.nome,
                  }),
                )
              },
            ),
          )

        const datasetsRecebidos =
          respostasDatasets.flatMap((resultado) =>
            resultado.status === "fulfilled"
              ? resultado.value
              : [],
          )

        datasetsRecebidos.sort(
          (a, b) =>
            new Date(b.dataImportacao).getTime() -
            new Date(a.dataImportacao).getTime(),
        )

        setDatasets(datasetsRecebidos)

        const ultimoProcessado =
          datasetsRecebidos.find(
            (dataset) =>
              dataset.estado
                .trim()
                .toLocaleLowerCase("pt-PT") ===
              "processado",
          )

        if (ultimoProcessado) {
          try {
            const response = await api.get(
              `/datasets/${ultimoProcessado.idDataset}/exploracao`,
            )

            setExploracao(
              response.data as ExploracaoDataset,
            )
          } catch {
            setExploracao(null)
          }
        } else {
          setExploracao(null)
        }
      } catch (requestError) {
        console.error(requestError)

        setError(
          "Nao foi possivel carregar o workspace. Confirme se a API esta ligada.",
        )
      } finally {
        setLoading(false)
        setRefreshing(false)
      }
    },
    [],
  )

  useEffect(() => {
    void carregarWorkspace()
  }, [carregarWorkspace])

  const projetosAtivos = projetos.length
  const totalDatasets = datasets.length

  const datasetsComScore = useMemo(
    () =>
      datasets.filter(
        (dataset) =>
          dataset.qualityScore !== null &&
          dataset.qualityScore !== undefined,
      ),
    [datasets],
  )

  const qualidadeMedia = useMemo(() => {
    if (datasetsComScore.length === 0) {
      return null
    }

    const total = datasetsComScore.reduce(
      (acc, dataset) =>
        acc + Number(dataset.qualityScore ?? 0),
      0,
    )

    return total / datasetsComScore.length
  }, [datasetsComScore])

  const ultimoDataset = datasets[0] ?? null

  const datasetsFiltrados = useMemo(() => {
    const termo = pesquisa
      .trim()
      .toLocaleLowerCase("pt-PT")

    if (!termo) {
      return datasets
    }

    return datasets.filter((dataset) => {
      return [
        dataset.nome,
        dataset.nomeOriginal,
        dataset.projetoNome,
        dataset.tipoArquivo,
        dataset.estado,
      ].some((valor) =>
        String(valor ?? "")
          .toLocaleLowerCase("pt-PT")
          .includes(termo),
      )
    })
  }, [datasets, pesquisa])

  const datasetsVisiveis =
    datasetsFiltrados.slice(0, 6)

  const primeiraParteNome =
    user?.nome?.trim().split(/\s+/)[0] ??
    "Daniel"

  if (loading) {
    return (
      <div className="data-home">
        <div className="workspace-loading">
          <LoaderCircle
            size={26}
            className="spin"
          />

          <strong>
            A preparar o seu workspace
          </strong>

          <span>
            A carregar projetos e datasets...
          </span>
        </div>
      </div>
    )
  }

  return (
    <div className="data-home">

      <header className="data-topbar">
        <div className="topbar-copy">
          <span className="workspace-kicker">
            DATA WORKSPACE
          </span>

          <h1>
            Bom dia, {primeiraParteNome}.
          </h1>

          <p>
            Os seus dados, prontos para explorar.
          </p>
        </div>

        <div className="topbar-actions">
          <button
            type="button"
            className="icon-action"
            title="Atualizar dados"
            onClick={() =>
              void carregarWorkspace(true)
            }
            disabled={refreshing}
          >
            <RefreshCw
              size={17}
              className={
                refreshing ? "spin" : ""
              }
            />
          </button>

          <button
            type="button"
            className="workspace-primary"
            onClick={() =>
              navigate("/projetos")
            }
          >
            <Upload size={17} />
            Importar dados
          </button>
        </div>
      </header>

      {error && (
        <div className="workspace-error">
          <AlertTriangle size={18} />

          <div>
            <strong>
              Nao foi possivel sincronizar os dados
            </strong>
            <span>{error}</span>
          </div>

          <button
            type="button"
            onClick={() =>
              void carregarWorkspace()
            }
          >
            Tentar novamente
          </button>
        </div>
      )}

      <section className="workspace-overview">

        <article className="continue-analysis">
          <div className="section-heading-row">
            <div>
              <span className="section-label">
                CONTINUAR ANALISE
              </span>

              <h2>
                {ultimoDataset
                  ? ultimoDataset.nome
                  : "Comece pelo seu primeiro dataset"}
              </h2>
            </div>

            <div className="dataset-file-icon">
              <FileSpreadsheet size={21} />
            </div>
          </div>

          {ultimoDataset ? (
            <>
              <div className="dataset-meta-line">
                <span>
                  {ultimoDataset.tipoArquivo.toUpperCase()}
                </span>

                <i />

                <span>
                  {formatarNumero(
                    ultimoDataset.totalRegistos,
                  )}{" "}
                  registos
                </span>

                <i />

                <span>
                  {formatarNumero(
                    ultimoDataset.totalColunas,
                  )}{" "}
                  colunas
                </span>
              </div>

              <div className="analysis-project">
                <FolderKanban size={15} />
                <span>
                  {ultimoDataset.projetoNome}
                </span>
              </div>

              <div className="analysis-footer">
                <div>
                  <small>
                    Ultima importacao
                  </small>

                  <strong>
                    {formatarData(
                      ultimoDataset.dataImportacao,
                    )}
                  </strong>
                </div>

                <button
                  type="button"
                  className="analysis-button"
                  onClick={() =>
                    navigate("/projetos")
                  }
                >
                  Abrir workspace
                  <ArrowRight size={16} />
                </button>
              </div>
            </>
          ) : (
            <div className="analysis-empty">
              <p>
                Crie um projeto e importe dados
                para iniciar uma analise.
              </p>

              <Link
                to="/projetos"
                className="analysis-button"
              >
                Criar projeto
                <ArrowRight size={16} />
              </Link>
            </div>
          )}
        </article>

        <article className="health-panel">
          <div className="section-heading-row">
            <div>
              <span className="section-label">
                DATA HEALTH
              </span>

              <h2>
                Qualidade dos dados
              </h2>
            </div>

            <ShieldCheck size={20} />
          </div>

          <div className="health-score">
            <strong>
              {formatarPercentual(
                exploracao?.qualityScore ??
                  ultimoDataset?.qualityScore ??
                  qualidadeMedia,
              )}
            </strong>

            <span>Quality Score</span>
          </div>

          <div className="health-bar">
            <span
              style={{
                width: `${Math.min(
                  100,
                  Math.max(
                    0,
                    Number(
                      exploracao?.qualityScore ??
                        ultimoDataset?.qualityScore ??
                        qualidadeMedia ??
                        0,
                    ),
                  ),
                )}%`,
              }}
            />
          </div>

          <div className="health-details">
            <div>
              <span>Ausentes</span>
              <strong>
                {exploracao
                  ? formatarNumero(
                      exploracao.qualidade
                        .totalValoresAusentes,
                    )
                  : "—"}
              </strong>
            </div>

            <div>
              <span>Duplicados</span>
              <strong>
                {exploracao
                  ? formatarNumero(
                      exploracao.qualidade
                        .totalDuplicados,
                    )
                  : "—"}
              </strong>
            </div>

            <div>
              <span>Outliers</span>
              <strong>
                {exploracao
                  ? formatarNumero(
                      exploracao.qualidade
                        .totalOutliers,
                    )
                  : "—"}
              </strong>
            </div>
          </div>

          {exploracao && (
            <div className="health-source">
              <CheckCircle2 size={13} />
              <span>
                {exploracao.nome}
              </span>
            </div>
          )}
        </article>

      </section>

      <section className="workspace-stats">
        <div>
          <span>Projetos ativos</span>
          <strong>
            {formatarNumero(projetosAtivos)}
          </strong>
        </div>

        <div>
          <span>Datasets</span>
          <strong>
            {formatarNumero(totalDatasets)}
          </strong>
        </div>

        <div>
          <span>Qualidade media</span>
          <strong>
            {formatarPercentual(
              qualidadeMedia,
            )}
          </strong>
        </div>

        <div>
          <span>Datasets processados</span>
          <strong>
            {formatarNumero(
              datasets.filter(
                (dataset) =>
                  dataset.estado
                    .trim()
                    .toLocaleLowerCase(
                      "pt-PT",
                    ) === "processado",
              ).length,
            )}
          </strong>
        </div>
      </section>

      <section className="datasets-section">

        <div className="datasets-heading">
          <div>
            <span className="section-label">
              SEUS DADOS
            </span>

            <h2>Datasets recentes</h2>
          </div>

          <div className="dataset-tools">
            <label className="dataset-search">
              <Search size={15} />

              <input
                value={pesquisa}
                onChange={(event) =>
                  setPesquisa(
                    event.target.value,
                  )
                }
                placeholder="Pesquisar datasets..."
              />
            </label>

            <Link
              to="/projetos"
              className="view-projects-link"
            >
              Ver projetos
              <ArrowRight size={14} />
            </Link>
          </div>
        </div>

        {datasetsVisiveis.length > 0 ? (
          <div className="dataset-table-shell">

            <div className="dataset-table-header">
              <span>Dataset</span>
              <span>Projeto</span>
              <span>Registos</span>
              <span>Colunas</span>
              <span>Qualidade</span>
              <span>Estado</span>
            </div>

            <div className="dataset-table-body">
              {datasetsVisiveis.map(
                (dataset) => (
                  <button
                    type="button"
                    className="dataset-row"
                    key={dataset.idDataset}
                    onClick={() =>
                      navigate("/projetos")
                    }
                  >
                    <span className="dataset-name-cell">
                      <span className="dataset-type-icon">
                        <Database size={15} />
                      </span>

                      <span>
                        <strong>
                          {dataset.nome}
                        </strong>

                        <small>
                          {dataset.tipoArquivo.toUpperCase()}
                          {" · "}
                          {formatarTamanho(
                            dataset.tamanhoBytes,
                          )}
                        </small>
                      </span>
                    </span>

                    <span className="table-secondary">
                      {dataset.projetoNome}
                    </span>

                    <span>
                      {formatarNumero(
                        dataset.totalRegistos,
                      )}
                    </span>

                    <span>
                      {formatarNumero(
                        dataset.totalColunas,
                      )}
                    </span>

                    <span className="score-cell">
                      {formatarPercentual(
                        dataset.qualityScore,
                      )}
                    </span>

                    <span>
                      <span
                        className={`status-pill ${estadoClasse(
                          dataset.estado,
                        )}`}
                      >
                        <i />
                        {dataset.estado}
                      </span>
                    </span>
                  </button>
                ),
              )}
            </div>

          </div>
        ) : (
          <div className="datasets-empty">
            <Database size={25} />

            <strong>
              {pesquisa
                ? "Nenhum dataset encontrado"
                : "Ainda nao existem datasets"}
            </strong>

            <span>
              {pesquisa
                ? "Experimente outro termo de pesquisa."
                : "Crie um projeto e importe o primeiro ficheiro."}
            </span>
          </div>
        )}

      </section>

      <section className="quick-actions-section">
        <div className="quick-heading">
          <span className="section-label">
            ACESSO RAPIDO
          </span>

          <h2>Continue o seu fluxo de dados</h2>
        </div>

        <div className="quick-actions">

          <button
            type="button"
            onClick={() =>
              navigate("/projetos")
            }
          >
            <span className="quick-icon">
              <Plus size={19} />
            </span>

            <span>
              <strong>Novo projeto</strong>
              <small>
                Organize uma nova analise
              </small>
            </span>

            <ArrowRight size={15} />
          </button>

          <button
            type="button"
            onClick={() =>
              navigate("/projetos")
            }
          >
            <span className="quick-icon">
              <Upload size={19} />
            </span>

            <span>
              <strong>Importar dataset</strong>
              <small>
                CSV e outros formatos
              </small>
            </span>

            <ArrowRight size={15} />
          </button>

          <button
            type="button"
            onClick={() => {
              if (ultimoDataset) {
                navigate("/projetos")
              }
            }}
            disabled={!ultimoDataset}
          >
            <span className="quick-icon">
              <Sparkles size={19} />
            </span>

            <span>
              <strong>Explorar dados</strong>
              <small>
                Continue a ultima analise
              </small>
            </span>

            <ArrowRight size={15} />
          </button>

        </div>
      </section>

    </div>
  )
}
