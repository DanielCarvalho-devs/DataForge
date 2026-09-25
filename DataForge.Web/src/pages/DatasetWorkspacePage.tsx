import {
  AlertTriangle,
  ArrowLeft,
  BarChart3,
  CheckCircle2,
  Database,
  Download,
  FileBarChart,
  FileSpreadsheet,
  History,
  LoaderCircle,
  RefreshCw,
  ShieldCheck,
  Sparkles,
  Table2,
  Wand2,
} from "lucide-react"

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react"

import {
  Link,
  useParams,
  useSearchParams,
} from "react-router-dom"

import { api } from "../api/client"
import { DatasetCleaningTab } from "../components/workspace/DatasetCleaningTab"
import {
  AnalisesTab,
  HistoricoTab,
  RelatoriosTab,
} from "../components/workspace/DatasetAdvancedTabs"

import {
  mensagemErro,
  normalizarLista,
  numero,
  percentagem,
} from "../components/workspace/workspaceTypes"

import type {
  Coluna,
  Dataset,
} from "../components/workspace/workspaceTypes"

type Tab =
  | "overview"
  | "dados"
  | "qualidade"
  | "exploracao"
  | "limpeza"
  | "analises"
  | "relatorios"
  | "historico"

const tabs: {
  id: Tab
  label: string
  icon: typeof Database
}[] = [
  { id: "overview", label: "Visao Geral", icon: Database },
  { id: "dados", label: "Dados", icon: Table2 },
  { id: "qualidade", label: "Qualidade", icon: ShieldCheck },
  { id: "exploracao", label: "Exploracao", icon: BarChart3 },
  { id: "limpeza", label: "Limpeza", icon: Wand2 },
  { id: "analises", label: "Analises", icon: Sparkles },
  { id: "relatorios", label: "Relatorios", icon: FileBarChart },
  { id: "historico", label: "Historico", icon: History },
]

function objeto(valor: unknown) {
  if (valor && typeof valor === "object") {
    return valor as Record<string, unknown>
  }

  return {}
}

function lerNumero(
  source: Record<string, unknown>,
  ...nomes: string[]
) {
  for (const nome of nomes) {
    const valor = source[nome]

    if (typeof valor === "number") return valor

    if (
      typeof valor === "string" &&
      valor.trim() !== "" &&
      Number.isFinite(Number(valor))
    ) {
      return Number(valor)
    }
  }

  return 0
}

export function DatasetWorkspacePage() {
  const { id } = useParams()
  const idDataset = Number(id)

  const [searchParams, setSearchParams] =
    useSearchParams()

  const tabParam =
    searchParams.get("tab") as Tab | null

  const tab: Tab =
    tabs.some((item) => item.id === tabParam)
      ? (tabParam as Tab)
      : "overview"

  const [dataset, setDataset] =
    useState<Dataset | null>(null)

  const [exploracao, setExploracao] =
    useState<Record<string, unknown>>({})

  const [colunas, setColunas] =
    useState<Coluna[]>([])

  const [qualidade, setQualidade] =
    useState<Record<string, unknown>>({})

  const [preview, setPreview] =
    useState<Record<string, unknown>>({})

  const [loading, setLoading] =
    useState(true)

  const [actionLoading, setActionLoading] =
    useState(false)

  const [erro, setErro] =
    useState<string | null>(null)

  const [exportOpen, setExportOpen] =
    useState(false)

  const [exportLoading, setExportLoading] =
    useState<"csv" | "xlsx" | null>(null)

  async function exportarDataset(
    formato: "csv" | "xlsx",
  ) {
    try {
      setExportLoading(formato)
      setErro(null)

      const response = await api.get(
        `/data-transfer/datasets/${idDataset}/exportar/${formato}`,
        {
          responseType: "blob",
        },
      )

      const blob = new Blob([response.data])

      const url =
        window.URL.createObjectURL(blob)

      const anchor =
        document.createElement("a")

      anchor.href = url

      anchor.download =
        `${dataset?.nome ?? "dataset"}.${formato}`

      document.body.appendChild(anchor)
      anchor.click()
      anchor.remove()

      window.URL.revokeObjectURL(url)

      setExportOpen(false)
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel exportar o dataset.",
        ),
      )
    } finally {
      setExportLoading(null)
    }
  }

  const [pagina, setPagina] =
    useState(1)

  const carregarBase = useCallback(async () => {
    if (!Number.isFinite(idDataset)) {
      setErro("Dataset invalido.")
      setLoading(false)
      return
    }

    try {
      setLoading(true)
      setErro(null)

      const [
        datasetResponse,
        exploracaoResponse,
        colunasResponse,
        qualidadeResponse,
      ] = await Promise.allSettled([
        api.get(`/datasets/${idDataset}`),
        api.get(`/datasets/${idDataset}/exploracao`),
        api.get(`/datasets/${idDataset}/colunas`),
        api.get(`/datasets/${idDataset}/qualidade`),
      ])

      if (datasetResponse.status === "fulfilled") {
        setDataset(datasetResponse.value.data)
      } else {
        throw datasetResponse.reason
      }

      if (exploracaoResponse.status === "fulfilled") {
        setExploracao(
          objeto(exploracaoResponse.value.data),
        )
      }

      if (colunasResponse.status === "fulfilled") {
        setColunas(
          normalizarLista<Coluna>(
            colunasResponse.value.data,
          ),
        )
      }

      if (qualidadeResponse.status === "fulfilled") {
        setQualidade(
          objeto(qualidadeResponse.value.data),
        )
      }
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel carregar o dataset.",
        ),
      )
    } finally {
      setLoading(false)
    }
  }, [idDataset])

  const carregarPreview =
    useCallback(async () => {
      try {
        const response =
          await api.get(
            `/datasets/${idDataset}/preview?pagina=${pagina}&tamanhoPagina=25`,
          )

        setPreview(objeto(response.data))
      } catch (error) {
        setErro(
          mensagemErro(
            error,
            "Nao foi possivel carregar o preview.",
          ),
        )
      }
    }, [idDataset, pagina])

  useEffect(() => {
    void carregarBase()
  }, [carregarBase])

  useEffect(() => {
    if (tab === "dados") {
      void carregarPreview()
    }
  }, [tab, carregarPreview])

  async function executarAnaliseCompleta() {
    try {
      setActionLoading(true)
      setErro(null)

      const endpoints = [
        `/datasets/${idDataset}/processar`,
        `/datasets/${idDataset}/analisar-qualidade`,
        `/datasets/${idDataset}/calcular-estatisticas`,
        `/datasets/${idDataset}/analisar-duplicados`,
        `/datasets/${idDataset}/analisar-outliers`,
      ]

      for (const endpoint of endpoints) {
        await api.post(endpoint)
      }

      await carregarBase()
    } catch (error) {
      setErro(
        mensagemErro(
          error,
          "Nao foi possivel concluir a analise.",
        ),
      )
    } finally {
      setActionLoading(false)
    }
  }

  const explorationObject =
    useMemo(
      () => ({
        ...exploracao,
        ...qualidade,
      }),
      [exploracao, qualidade],
    )

  const qualityScore =
    dataset?.qualityScore ??
    lerNumero(
      explorationObject,
      "qualityScore",
      "qualidade",
      "scoreQualidade",
    )

  const ausentes =
    lerNumero(
      explorationObject,
      "totalValoresNulos",
      "valoresNulos",
      "missingValues",
      "ausentes",
      "totalAusentes",
    )

  const duplicados =
    lerNumero(
      explorationObject,
      "totalDuplicados",
      "duplicados",
      "duplicateRows",
    )

  const outliers =
    lerNumero(
      explorationObject,
      "totalOutliers",
      "outliers",
      "anomalias",
      "totalAnomalias",
    )

  if (loading) {
    return (
      <div className="df-loading">
        <LoaderCircle className="spin" size={28} />
        <strong>A preparar workspace</strong>
      </div>
    )
  }

  if (erro && !dataset) {
    return (
      <div className="df-error-state">
        <AlertTriangle size={32} />
        <h2>Dataset indisponivel</h2>
        <p>{erro}</p>
        <Link to="/projetos">Voltar aos projetos</Link>
      </div>
    )
  }

  if (!dataset) return null

  return (
    <div className="df-workspace-page dataset-workspace">

      <header className="df-dataset-header">
        <div>
          <Link
            className="df-back-link"
            to={`/projetos/${dataset.idProjeto}`}
          >
            <ArrowLeft size={14} />
            Projeto
          </Link>

          <span className="df-kicker">
            DATASET WORKSPACE
          </span>

          <div className="df-title-line">
            <span className="df-big-file-icon">
              <FileSpreadsheet size={23} />
            </span>

            <div>
              <h1>{dataset.nome}</h1>

              <p>
                {numero(dataset.totalRegistos)} registos
                {" · "}
                {numero(dataset.totalColunas)} colunas
                {" · "}
                {dataset.estado ?? "Dataset"}
              </p>
            </div>
          </div>
        </div>

        <div className="df-header-actions">
          <button
            type="button"
            className="df-icon-button"
            onClick={() => void carregarBase()}
          >
            <RefreshCw size={17} />
          </button>

          <div className="df-export-wrapper">
            <button
              type="button"
              className="df-secondary-button"
              onClick={() =>
                setExportOpen((value) => !value)
              }
            >
              <Download size={16} />
              Exportar
            </button>

            {exportOpen && (
              <div className="df-export-menu">
                <button
                  type="button"
                  disabled={exportLoading !== null}
                  onClick={() =>
                    void exportarDataset("csv")
                  }
                >
                  <FileSpreadsheet size={15} />
                  <span>
                    <strong>CSV</strong>
                    <small>
                      Valores separados por vírgulas
                    </small>
                  </span>
                </button>

                <button
                  type="button"
                  disabled={exportLoading !== null}
                  onClick={() =>
                    void exportarDataset("xlsx")
                  }
                >
                  <FileSpreadsheet size={15} />
                  <span>
                    <strong>Excel</strong>
                    <small>
                      Workbook XLSX
                    </small>
                  </span>
                </button>
              </div>
            )}
          </div>

          <button
            type="button"
            className="df-primary-button"
            disabled={actionLoading}
            onClick={() =>
              void executarAnaliseCompleta()
            }
          >
            {actionLoading ? (
              <LoaderCircle
                size={16}
                className="spin"
              />
            ) : (
              <Sparkles size={16} />
            )}

            {actionLoading
              ? "A analisar..."
              : "Analisar dataset"}
          </button>
        </div>
      </header>

      {erro && (
        <div className="df-inline-error">
          <AlertTriangle size={16} />
          {erro}
        </div>
      )}

      <nav className="df-tabs">
        {tabs.map((item) => {
          const Icon = item.icon

          return (
            <button
              type="button"
              key={item.id}
              className={
                tab === item.id ? "active" : ""
              }
              onClick={() =>
                setSearchParams({ tab: item.id })
              }
            >
              <Icon size={15} />
              {item.label}
            </button>
          )
        })}
      </nav>

      {tab === "overview" && (
        <Overview
          dataset={dataset}
          qualityScore={qualityScore}
          ausentes={ausentes}
          duplicados={duplicados}
          outliers={outliers}
          colunas={colunas}
        />
      )}

      {tab === "dados" && (
        <Dados
          preview={preview}
          pagina={pagina}
          setPagina={setPagina}
        />
      )}

      {tab === "qualidade" && (
        <Qualidade
          score={qualityScore}
          ausentes={ausentes}
          duplicados={duplicados}
          outliers={outliers}
          qualidade={qualidade}
        />
      )}

      {tab === "exploracao" && (
        <Exploracao
          colunas={colunas}
          idDataset={idDataset}
        />
      )}

      {tab === "limpeza" && (
        <DatasetCleaningTab
          idDataset={idDataset}
        />
      )}

      {tab === "analises" && (
        <AnalisesTab
          idDataset={idDataset}
        />
      )}

      {tab === "relatorios" && (
        <RelatoriosTab
          idDataset={idDataset}
        />
      )}

      {tab === "historico" && (
        <HistoricoTab
          idDataset={idDataset}
        />
      )}
    </div>
  )
}

function Overview({
  dataset,
  qualityScore,
  ausentes,
  duplicados,
  outliers,
  colunas,
}: {
  dataset: Dataset
  qualityScore: number
  ausentes: number
  duplicados: number
  outliers: number
  colunas: Coluna[]
}) {
  return (
    <div className="df-tab-content">
      <section className="df-kpi-grid">
        <Kpi
          icon={Database}
          label="Registos"
          value={numero(dataset.totalRegistos)}
        />

        <Kpi
          icon={Table2}
          label="Colunas"
          value={numero(dataset.totalColunas)}
        />

        <Kpi
          icon={ShieldCheck}
          label="Qualidade"
          value={percentagem(qualityScore)}
        />

        <Kpi
          icon={AlertTriangle}
          label="Problemas"
          value={numero(
            ausentes + duplicados + outliers,
          )}
        />
      </section>

      <section className="df-overview-grid">
        <article className="df-panel df-health-panel">
          <span className="df-kicker">
            DATA HEALTH
          </span>

          <div className="df-health-number">
            {percentagem(qualityScore)}
          </div>

          <div className="df-health-track">
            <i
              style={{
                width: `${Math.max(
                  0,
                  Math.min(100, qualityScore),
                )}%`,
              }}
            />
          </div>

          <div className="df-health-items">
            <span>
              Ausentes
              <strong>{numero(ausentes)}</strong>
            </span>

            <span>
              Duplicados
              <strong>{numero(duplicados)}</strong>
            </span>

            <span>
              Outliers
              <strong>{numero(outliers)}</strong>
            </span>
          </div>
        </article>

        <article className="df-panel">
          <div className="df-panel-heading">
            <div>
              <span className="df-kicker">
                SCHEMA
              </span>
              <h2>Colunas</h2>
            </div>
          </div>

          <div className="df-schema-list">
            {colunas.slice(0, 8).map((coluna) => (
              <div key={coluna.idColuna}>
                <strong>{coluna.nome}</strong>
                <span>
                  {coluna.tipoDado ??
                    coluna.tipo ??
                    "—"}
                </span>
                <em>
                  {coluna.papelSemantico ??
                    coluna.semanticRole ??
                    ""}
                </em>
              </div>
            ))}
          </div>
        </article>
      </section>
    </div>
  )
}

function Kpi({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Database
  label: string
  value: string
}) {
  return (
    <article className="df-kpi">
      <Icon size={19} />
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  )
}

function Dados({
  preview,
  pagina,
  setPagina,
}: {
  preview: Record<string, unknown>
  pagina: number
  setPagina: (pagina: number) => void
}) {
  const linhas =
    normalizarLista<Record<string, unknown>>(preview)

  const colunas =
    linhas.length > 0
      ? Object.keys(linhas[0])
      : []

  return (
    <div className="df-tab-content">
      <section className="df-panel">
        <div className="df-panel-heading">
          <div>
            <span className="df-kicker">
              DATA PREVIEW
            </span>
            <h2>Registos</h2>
          </div>

          <span className="df-page-label">
            Pagina {pagina}
          </span>
        </div>

        {linhas.length === 0 ? (
          <div className="df-empty compact">
            Nenhum registo recebido no preview.
          </div>
        ) : (
          <div className="df-table-scroll">
            <table className="df-table">
              <thead>
                <tr>
                  {colunas.map((coluna) => (
                    <th key={coluna}>{coluna}</th>
                  ))}
                </tr>
              </thead>

              <tbody>
                {linhas.map((linha, index) => (
                  <tr key={index}>
                    {colunas.map((coluna) => (
                      <td key={coluna}>
                        {String(linha[coluna] ?? "")}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="df-pagination">
          <button
            type="button"
            disabled={pagina <= 1}
            onClick={() =>
              setPagina(Math.max(1, pagina - 1))
            }
          >
            Anterior
          </button>

          <button
            type="button"
            onClick={() =>
              setPagina(pagina + 1)
            }
          >
            Seguinte
          </button>
        </div>
      </section>
    </div>
  )
}

function Qualidade({
  score,
  ausentes,
  duplicados,
  outliers,
  qualidade,
}: {
  score: number
  ausentes: number
  duplicados: number
  outliers: number
  qualidade: Record<string, unknown>
}) {
  const problemas =
    normalizarLista<Record<string, unknown>>(qualidade)

  return (
    <div className="df-tab-content">
      <section className="df-kpi-grid">
        <Kpi
          icon={ShieldCheck}
          label="Quality score"
          value={percentagem(score)}
        />

        <Kpi
          icon={AlertTriangle}
          label="Ausentes"
          value={numero(ausentes)}
        />

        <Kpi
          icon={Database}
          label="Duplicados"
          value={numero(duplicados)}
        />

        <Kpi
          icon={BarChart3}
          label="Outliers"
          value={numero(outliers)}
        />
      </section>

      <section className="df-panel">
        <div className="df-panel-heading">
          <div>
            <span className="df-kicker">
              QUALITY ENGINE
            </span>
            <h2>Diagnostico</h2>
          </div>
        </div>

        {problemas.length === 0 ? (
          <div className="df-quality-ok">
            <CheckCircle2 size={22} />
            <div>
              <strong>Diagnostico carregado</strong>
              <p>
                Os indicadores de qualidade acima
                representam a analise disponivel.
              </p>
            </div>
          </div>
        ) : (
          <div className="df-issue-list">
            {problemas.map((problema, index) => (
              <div key={index}>
                <AlertTriangle size={15} />
                <span>
                  {String(
                    problema.descricao ??
                      problema.tipo ??
                      "Problema de qualidade",
                  )}
                </span>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}

function Exploracao({
  colunas,
  idDataset,
}: {
  colunas: Coluna[]
  idDataset: number
}) {
  const [selecionada, setSelecionada] =
    useState<Coluna | null>(colunas[0] ?? null)

  const [resumo, setResumo] =
    useState<Record<string, unknown>>({})

  const [distribuicao, setDistribuicao] =
    useState<Record<string, unknown>[]>([])

  const [loading, setLoading] =
    useState(false)

  useEffect(() => {
    if (!selecionada) return

    async function carregarColuna() {
      try {
        setLoading(true)

        const [resumoResponse, distribuicaoResponse] =
          await Promise.allSettled([
            api.get(
              `/datasets/${idDataset}/exploracao/colunas/${selecionada!.idColuna}/resumo`,
            ),
            api.get(
              `/datasets/${idDataset}/exploracao/colunas/${selecionada!.idColuna}/distribuicao?top=10`,
            ),
          ])

        if (resumoResponse.status === "fulfilled") {
          setResumo(objeto(resumoResponse.value.data))
        }

        if (
          distribuicaoResponse.status === "fulfilled"
        ) {
          setDistribuicao(
            normalizarLista<Record<string, unknown>>(
              distribuicaoResponse.value.data,
            ),
          )
        }
      } finally {
        setLoading(false)
      }
    }

    void carregarColuna()
  }, [selecionada, idDataset])

  return (
    <div className="df-tab-content">
      <div className="df-exploration-layout">
        <section className="df-panel">
          <div className="df-panel-heading">
            <div>
              <span className="df-kicker">
                COLUMNS
              </span>
              <h2>Explorar colunas</h2>
            </div>
          </div>

          <div className="df-column-list">
            {colunas.map((coluna) => (
              <button
                type="button"
                key={coluna.idColuna}
                className={
                  selecionada?.idColuna ===
                  coluna.idColuna
                    ? "active"
                    : ""
                }
                onClick={() =>
                  setSelecionada(coluna)
                }
              >
                <span>{coluna.nome}</span>
                <small>
                  {coluna.tipoDado ??
                    coluna.tipo ??
                    "—"}
                </small>
              </button>
            ))}
          </div>
        </section>

        <section className="df-panel">
          <div className="df-panel-heading">
            <div>
              <span className="df-kicker">
                COLUMN PROFILE
              </span>
              <h2>
                {selecionada?.nome ??
                  "Selecione uma coluna"}
              </h2>
            </div>

            {loading && (
              <LoaderCircle
                size={17}
                className="spin"
              />
            )}
          </div>

          {selecionada && (
            <>
              <div className="df-profile-grid">
                {Object.entries(resumo)
                  .filter(
                    ([, valor]) =>
                      typeof valor !== "object",
                  )
                  .slice(0, 8)
                  .map(([chave, valor]) => (
                    <div key={chave}>
                      <span>{chave}</span>
                      <strong>
                        {String(valor ?? "—")}
                      </strong>
                    </div>
                  ))}
              </div>

              <div className="df-distribution">
                <h3>Distribuicao</h3>

                {distribuicao.length === 0 ? (
                  <p>
                    Sem distribuicao disponivel.
                  </p>
                ) : (
                  distribuicao.map((item, index) => (
                    <div
                      className="df-distribution-row"
                      key={index}
                    >
                      <span>
                        {String(
                          item.valor ??
                            item.label ??
                            item.nome ??
                            `Valor ${index + 1}`,
                        )}
                      </span>

                      <strong>
                        {String(
                          item.quantidade ??
                            item.total ??
                            item.count ??
                            "",
                        )}
                      </strong>
                    </div>
                  ))
                )}
              </div>
            </>
          )}
        </section>
      </div>
    </div>
  )
}


