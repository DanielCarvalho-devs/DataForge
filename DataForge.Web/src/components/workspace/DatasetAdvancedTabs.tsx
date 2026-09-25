import {
  AlertTriangle,
  BarChart3,
  CheckCircle2,
  Clock3,
  Database,
  FileBarChart,
  FileText,
  History,
  LoaderCircle,
  Plus,
  Printer,
  RefreshCw,
  ShieldCheck,
  Sparkles,
  Trash2,
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

import { api } from "../../api/client"

type Analise = {
  idAnalise: number
  idDataset: number
  nome: string
  tipo: string
  configuracaoJson?: string | null
  resultadoJson?: string | null
  dataCriacao: string
  dataAtualizacao?: string | null
}

type Historico = {
  idHistorico: number
  acao: string
  entidade?: string | null
  idRegisto?: number | null
  descricao?: string | null
  dataAcao: string
}

type RelatorioDataset = {
  idDataset?: number
  nome?: string
  nomeOriginal?: string
  tipoArquivo?: string
  tamanhoBytes?: number | null
  totalRegistos?: number
  totalColunas?: number
  qualityScore?: number | null
  estado?: string
  dataImportacao?: string
  dataProcessamento?: string | null
}

type RelatorioResumo = {
  totalProblemas?: number
  totalAnomalias?: number
  totalAnalises?: number
  colunasValidas?: number
}

type ProblemaTipo = {
  tipo?: string
  quantidade?: number
}

type ColunaRelatorio = {
  idColuna?: number
  nome?: string
  tipoDetectado?: string | null
  tipoOriginal?: string | null
  percentualValido?: number | null
}

type Relatorio = {
  dataset?: RelatorioDataset
  resumo?: RelatorioResumo
  problemasPorTipo?: ProblemaTipo[]
  colunas?: ColunaRelatorio[]
  geradoEm?: string
}

function lista<T>(value: unknown): T[] {
  if (Array.isArray(value)) {
    return value as T[]
  }

  if (
    value &&
    typeof value === "object"
  ) {
    const obj =
      value as Record<string, unknown>

    for (
      const key of [
        "items",
        "dados",
        "data",
        "resultados",
      ]
    ) {
      if (Array.isArray(obj[key])) {
        return obj[key] as T[]
      }
    }
  }

  return []
}

function erroMensagem(
  error: unknown,
  fallback: string,
) {
  if (
    error &&
    typeof error === "object" &&
    "response" in error
  ) {
    const response =
      (
        error as {
          response?: {
            data?: unknown
          }
        }
      ).response

    if (typeof response?.data === "string") {
      return response.data
    }

    if (
      response?.data &&
      typeof response.data === "object"
    ) {
      const data =
        response.data as Record<string, unknown>

      if (typeof data.message === "string") {
        return data.message
      }

      if (typeof data.mensagem === "string") {
        return data.mensagem
      }

      if (typeof data.title === "string") {
        return data.title
      }
    }
  }

  return fallback
}

function dataHora(value?: string | null) {
  if (!value) return "—"

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat(
    "pt-PT",
    {
      dateStyle: "medium",
      timeStyle: "short",
    },
  ).format(date)
}

function numero(
  value?: number | null,
) {
  return new Intl.NumberFormat(
    "pt-PT",
  ).format(value ?? 0)
}

function percentagem(
  value?: number | null,
) {
  return `${Number(value ?? 0)
    .toFixed(2)
    .replace(".", ",")}%`
}

function parseResultado(
  json?: string | null,
): Record<string, unknown> {
  if (!json) return {}

  try {
    const value = JSON.parse(json)

    if (
      value &&
      typeof value === "object"
    ) {
      return value as Record<string, unknown>
    }
  } catch {
    return {}
  }

  return {}
}

/* ============================================================
   ANALISES
============================================================ */

export function AnalisesTab({
  idDataset,
}: {
  idDataset: number
}) {
  const [analises, setAnalises] =
    useState<Analise[]>([])

  const [nome, setNome] =
    useState("")

  const [loading, setLoading] =
    useState(true)

  const [creating, setCreating] =
    useState(false)

  const [erro, setErro] =
    useState<string | null>(null)

  const carregar =
    useCallback(async () => {
      try {
        setLoading(true)
        setErro(null)

        const response =
          await api.get(
            `/dataset-intelligence/datasets/${idDataset}/analises`,
          )

        setAnalises(
          lista<Analise>(response.data),
        )
      } catch (error) {
        setErro(
          erroMensagem(
            error,
            "Não foi possível carregar as análises.",
          ),
        )
      } finally {
        setLoading(false)
      }
    }, [idDataset])

  useEffect(() => {
    void carregar()
  }, [carregar])

  async function criar(
    event: FormEvent,
  ) {
    event.preventDefault()

    try {
      setCreating(true)
      setErro(null)

      await api.post(
        `/dataset-intelligence/datasets/${idDataset}/analises`,
        {
          nome:
            nome.trim() ||
            `Análise ${new Date().toLocaleDateString(
              "pt-PT",
            )}`,
        },
      )

      setNome("")
      await carregar()
    } catch (error) {
      setErro(
        erroMensagem(
          error,
          "Não foi possível criar a análise.",
        ),
      )
    } finally {
      setCreating(false)
    }
  }

  async function eliminar(
    analise: Analise,
  ) {
    const confirmar =
      window.confirm(
        `Eliminar a análise "${analise.nome}"?`,
      )

    if (!confirmar) return

    try {
      setErro(null)

      await api.delete(
        `/dataset-intelligence/datasets/${idDataset}/analises/${analise.idAnalise}`,
      )

      await carregar()
    } catch (error) {
      setErro(
        erroMensagem(
          error,
          "Não foi possível eliminar a análise.",
        ),
      )
    }
  }

  return (
    <div className="df-tab-content">
      <section className="df-advanced-hero">
        <div>
          <span className="df-kicker">
            ANALYTICS
          </span>

          <h2>Análises do dataset</h2>

          <p>
            Guarde snapshots analíticos do estado
            atual dos dados e acompanhe a evolução
            da qualidade ao longo do trabalho.
          </p>
        </div>

        <div className="df-advanced-hero-icon">
          <Sparkles size={28} />
        </div>
      </section>

      {erro && (
        <div className="df-inline-error">
          <AlertTriangle size={17} />
          {erro}
        </div>
      )}

      <section className="df-panel">
        <div className="df-panel-heading">
          <div>
            <span className="df-kicker">
              NOVA ANÁLISE
            </span>

            <h2>Criar snapshot</h2>
          </div>
        </div>

        <form
          className="df-analysis-create"
          onSubmit={criar}
        >
          <input
            value={nome}
            maxLength={200}
            onChange={(event) =>
              setNome(event.target.value)
            }
            placeholder="Ex.: Revisão de qualidade setembro"
          />

          <button
            className="df-primary-button"
            type="submit"
            disabled={creating}
          >
            {creating ? (
              <LoaderCircle
                size={17}
                className="spin"
              />
            ) : (
              <Plus size={17} />
            )}

            {creating
              ? "A criar..."
              : "Criar análise"}
          </button>
        </form>
      </section>

      <section className="df-panel">
        <div className="df-panel-heading">
          <div>
            <span className="df-kicker">
              ANÁLISES GUARDADAS
            </span>

            <h2>
              Histórico analítico
            </h2>
          </div>

          <button
            type="button"
            className="df-icon-button"
            onClick={() => void carregar()}
            title="Atualizar"
          >
            <RefreshCw size={17} />
          </button>
        </div>

        {loading ? (
          <div className="df-advanced-loading">
            <LoaderCircle
              className="spin"
              size={22}
            />
            A carregar análises...
          </div>
        ) : analises.length === 0 ? (
          <div className="df-advanced-empty">
            <BarChart3 size={28} />

            <strong>
              Ainda não existem análises
            </strong>

            <p>
              Crie a primeira análise para
              guardar o estado atual do dataset.
            </p>
          </div>
        ) : (
          <div className="df-analysis-list">
            {analises.map((analise) => {
              const resultado =
                parseResultado(
                  analise.resultadoJson,
                )

              return (
                <article
                  key={analise.idAnalise}
                  className="df-analysis-card"
                >
                  <div className="df-analysis-card-top">
                    <div>
                      <span className="df-analysis-icon">
                        <BarChart3 size={18} />
                      </span>

                      <div>
                        <strong>
                          {analise.nome}
                        </strong>

                        <small>
                          {dataHora(
                            analise.dataCriacao,
                          )}
                        </small>
                      </div>
                    </div>

                    <button
                      type="button"
                      className="df-danger-icon"
                      onClick={() =>
                        void eliminar(analise)
                      }
                      title="Eliminar análise"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>

                  <div className="df-analysis-metrics">
                    <div>
                      <span>Registos</span>
                      <strong>
                        {numero(
                          Number(
                            resultado.totalRegistos ??
                            resultado.TotalRegistos ??
                            0,
                          ),
                        )}
                      </strong>
                    </div>

                    <div>
                      <span>Colunas</span>
                      <strong>
                        {numero(
                          Number(
                            resultado.totalColunas ??
                            resultado.TotalColunas ??
                            0,
                          ),
                        )}
                      </strong>
                    </div>

                    <div>
                      <span>Qualidade</span>
                      <strong>
                        {percentagem(
                          Number(
                            resultado.qualityScore ??
                            resultado.QualityScore ??
                            0,
                          ),
                        )}
                      </strong>
                    </div>

                    <div>
                      <span>Problemas</span>
                      <strong>
                        {numero(
                          Number(
                            resultado.totalProblemas ??
                            resultado.TotalProblemas ??
                            0,
                          ),
                        )}
                      </strong>
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </section>
    </div>
  )
}

/* ============================================================
   RELATORIOS
============================================================ */

export function RelatoriosTab({
  idDataset,
}: {
  idDataset: number
}) {
  const [relatorio, setRelatorio] =
    useState<Relatorio | null>(null)

  const [loading, setLoading] =
    useState(true)

  const [generating, setGenerating] =
    useState(false)

  const [erro, setErro] =
    useState<string | null>(null)

  const carregar =
    useCallback(async () => {
      try {
        setLoading(true)
        setErro(null)

        const response =
          await api.get(
            `/dataset-intelligence/datasets/${idDataset}/relatorio`,
          )

        setRelatorio(
          response.data as Relatorio,
        )
      } catch (error) {
        setErro(
          erroMensagem(
            error,
            "Não foi possível carregar o relatório.",
          ),
        )
      } finally {
        setLoading(false)
      }
    }, [idDataset])

  useEffect(() => {
    void carregar()
  }, [carregar])

  async function gerar() {
    try {
      setGenerating(true)
      setErro(null)

      await api.post(
        `/dataset-intelligence/datasets/${idDataset}/relatorio/gerar`,
      )

      await carregar()
    } catch (error) {
      setErro(
        erroMensagem(
          error,
          "Não foi possível gerar o relatório.",
        ),
      )
    } finally {
      setGenerating(false)
    }
  }

  function imprimir() {
    window.print()
  }

  if (loading) {
    return (
      <div className="df-advanced-loading">
        <LoaderCircle
          className="spin"
          size={24}
        />
        A preparar relatório...
      </div>
    )
  }

  const dataset =
    relatorio?.dataset ?? {}

  const resumo =
    relatorio?.resumo ?? {}

  const problemas =
    relatorio?.problemasPorTipo ?? []

  const colunas =
    relatorio?.colunas ?? []

  return (
    <div className="df-tab-content df-report-area">
      <section className="df-advanced-hero">
        <div>
          <span className="df-kicker">
            DATA REPORT
          </span>

          <h2>
            Relatório do dataset
          </h2>

          <p>
            Resumo consolidado da estrutura,
            qualidade e atividade analítica
            deste dataset.
          </p>
        </div>

        <div className="df-report-actions">
          <button
            type="button"
            className="df-secondary-button"
            onClick={imprimir}
          >
            <Printer size={17} />
            Imprimir / PDF
          </button>

          <button
            type="button"
            className="df-primary-button"
            disabled={generating}
            onClick={() => void gerar()}
          >
            {generating ? (
              <LoaderCircle
                className="spin"
                size={17}
              />
            ) : (
              <FileBarChart size={17} />
            )}

            {generating
              ? "A gerar..."
              : "Gerar relatório"}
          </button>
        </div>
      </section>

      {erro && (
        <div className="df-inline-error">
          <AlertTriangle size={17} />
          {erro}
        </div>
      )}

      <section className="df-report-sheet">
        <header className="df-report-header">
          <div>
            <span>
              DATAFORGE REPORT
            </span>

            <h1>
              {dataset.nome ??
                "Dataset"}
            </h1>

            <p>
              Relatório técnico de dados
            </p>
          </div>

          <FileText size={34} />
        </header>

        <div className="df-report-kpis">
          <ReportMetric
            label="Registos"
            value={numero(
              dataset.totalRegistos,
            )}
          />

          <ReportMetric
            label="Colunas"
            value={numero(
              dataset.totalColunas,
            )}
          />

          <ReportMetric
            label="Qualidade"
            value={percentagem(
              dataset.qualityScore,
            )}
          />

          <ReportMetric
            label="Problemas"
            value={numero(
              resumo.totalProblemas,
            )}
          />

          <ReportMetric
            label="Anomalias"
            value={numero(
              resumo.totalAnomalias,
            )}
          />

          <ReportMetric
            label="Análises"
            value={numero(
              resumo.totalAnalises,
            )}
          />
        </div>

        <div className="df-report-grid">
          <section>
            <h3>
              Informações do dataset
            </h3>

            <ReportRow
              label="Nome original"
              value={
                dataset.nomeOriginal ?? "—"
              }
            />

            <ReportRow
              label="Formato"
              value={
                dataset.tipoArquivo ?? "—"
              }
            />

            <ReportRow
              label="Estado"
              value={
                dataset.estado ?? "—"
              }
            />

            <ReportRow
              label="Importação"
              value={dataHora(
                dataset.dataImportacao,
              )}
            />

            <ReportRow
              label="Processamento"
              value={dataHora(
                dataset.dataProcessamento,
              )}
            />
          </section>

          <section>
            <h3>
              Qualidade
            </h3>

            {problemas.length === 0 ? (
              <div className="df-report-ok">
                <CheckCircle2 size={18} />
                Nenhum problema ativo
                agrupado por tipo.
              </div>
            ) : (
              problemas.map(
                (item, index) => (
                  <ReportRow
                    key={index}
                    label={
                      item.tipo ??
                      "Problema"
                    }
                    value={numero(
                      item.quantidade,
                    )}
                  />
                ),
              )
            )}
          </section>
        </div>

        <section className="df-report-columns">
          <h3>
            Estrutura das colunas
          </h3>

          <div className="df-table-scroll">
            <table className="df-table">
              <thead>
                <tr>
                  <th>Coluna</th>
                  <th>Tipo detetado</th>
                  <th>Tipo original</th>
                  <th>Validade</th>
                </tr>
              </thead>

              <tbody>
                {colunas.map(
                  (coluna, index) => (
                    <tr
                      key={
                        coluna.idColuna ??
                        index
                      }
                    >
                      <td>
                        <strong>
                          {coluna.nome ??
                            "—"}
                        </strong>
                      </td>

                      <td>
                        {coluna.tipoDetectado ??
                          "—"}
                      </td>

                      <td>
                        {coluna.tipoOriginal ??
                          "—"}
                      </td>

                      <td>
                        {percentagem(
                          coluna.percentualValido,
                        )}
                      </td>
                    </tr>
                  ),
                )}
              </tbody>
            </table>
          </div>
        </section>

        <footer className="df-report-footer">
          <span>
            Gerado pelo DataForge
          </span>

          <span>
            {dataHora(
              relatorio?.geradoEm,
            )}
          </span>
        </footer>
      </section>
    </div>
  )
}

function ReportMetric({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function ReportRow({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className="df-report-row">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

/* ============================================================
   HISTORICO
============================================================ */

export function HistoricoTab({
  idDataset,
}: {
  idDataset: number
}) {
  const [eventos, setEventos] =
    useState<Historico[]>([])

  const [loading, setLoading] =
    useState(true)

  const [erro, setErro] =
    useState<string | null>(null)

  const carregar =
    useCallback(async () => {
      try {
        setLoading(true)
        setErro(null)

        const response =
          await api.get(
            `/dataset-intelligence/datasets/${idDataset}/historico`,
          )

        setEventos(
          lista<Historico>(response.data),
        )
      } catch (error) {
        setErro(
          erroMensagem(
            error,
            "Não foi possível carregar o histórico.",
          ),
        )
      } finally {
        setLoading(false)
      }
    }, [idDataset])

  useEffect(() => {
    void carregar()
  }, [carregar])

  const grupos =
    useMemo(() => {
      const map =
        new Map<
          string,
          Historico[]
        >()

      for (const evento of eventos) {
        const date =
          new Date(evento.dataAcao)

        const chave =
          Number.isNaN(date.getTime())
            ? "Outros"
            : new Intl.DateTimeFormat(
                "pt-PT",
                {
                  dateStyle: "long",
                },
              ).format(date)

        const atual =
          map.get(chave) ?? []

        atual.push(evento)
        map.set(chave, atual)
      }

      return Array.from(
        map.entries(),
      )
    }, [eventos])

  return (
    <div className="df-tab-content">
      <section className="df-advanced-hero">
        <div>
          <span className="df-kicker">
            ACTIVITY LOG
          </span>

          <h2>
            Histórico do dataset
          </h2>

          <p>
            Timeline das operações registadas
            neste dataset.
          </p>
        </div>

        <button
          type="button"
          className="df-secondary-button"
          onClick={() => void carregar()}
        >
          <RefreshCw size={17} />
          Atualizar
        </button>
      </section>

      {erro && (
        <div className="df-inline-error">
          <AlertTriangle size={17} />
          {erro}
        </div>
      )}

      <section className="df-panel">
        {loading ? (
          <div className="df-advanced-loading">
            <LoaderCircle
              className="spin"
              size={22}
            />
            A carregar histórico...
          </div>
        ) : grupos.length === 0 ? (
          <div className="df-advanced-empty">
            <History size={29} />

            <strong>
              Ainda não existem eventos
            </strong>

            <p>
              Novas análises e relatórios
              aparecerão automaticamente aqui.
            </p>
          </div>
        ) : (
          <div className="df-history-groups">
            {grupos.map(
              ([data, items]) => (
                <div
                  className="df-history-group"
                  key={data}
                >
                  <div className="df-history-date">
                    {data}
                  </div>

                  <div className="df-history-timeline">
                    {items.map(
                      (evento) => (
                        <article
                          className="df-history-event"
                          key={
                            evento.idHistorico
                          }
                        >
                          <div className="df-history-marker">
                            <Clock3 size={15} />
                          </div>

                          <div className="df-history-event-content">
                            <div>
                              <strong>
                                {evento.acao}
                              </strong>

                              <time>
                                {dataHora(
                                  evento.dataAcao,
                                )}
                              </time>
                            </div>

                            {evento.descricao && (
                              <p>
                                {
                                  evento.descricao
                                }
                              </p>
                            )}

                            {evento.entidade && (
                              <span className="df-history-entity">
                                {
                                  evento.entidade
                                }
                              </span>
                            )}
                          </div>
                        </article>
                      ),
                    )}
                  </div>
                </div>
              ),
            )}
          </div>
        )}
      </section>
    </div>
  )
}

/* Keep imports represented in the compiled UI */
void Database
void ShieldCheck
