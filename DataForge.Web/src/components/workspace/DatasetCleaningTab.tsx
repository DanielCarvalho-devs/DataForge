import {
  AlertTriangle,
  Check,
  CheckCircle2,
  Clock3,
  Eraser,
  LoaderCircle,
  Plus,
  RefreshCw,
  Sparkles,
  Trash2,
  Type,
  WandSparkles,
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

type Coluna = {
  idColuna: number
  nome: string
  tipoDetectado?: string | null
  tipoOriginal?: string | null
  percentualValido?: number | null
}

type Transformacao = {
  idTransformacao: number
  idDataset: number
  idColuna?: number | null
  coluna?: string | null
  tipo: string
  descricao?: string | null
  parametrosJson?: string | null
  registosAfetados?: number | null
  estado: string
  dataCriacao: string
  dataExecucao?: string | null
}

type TipoTransformacao =
  | "RemoverDuplicados"
  | "TratarAusentes"
  | "NormalizarTexto"
  | "ConverterTipo"

function lista<T>(
  value: unknown,
): T[] {
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
        response.data as Record<
          string,
          unknown
        >

      if (
        typeof data.mensagem === "string"
      ) {
        return data.mensagem
      }

      if (
        typeof data.message === "string"
      ) {
        return data.message
      }

      if (
        typeof data.title === "string"
      ) {
        return data.title
      }
    }
  }

  return fallback
}

function dataHora(
  value?: string | null,
) {
  if (!value) return "—"

  const date =
    new Date(value)

  if (
    Number.isNaN(
      date.getTime(),
    )
  ) {
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

function nomeTipo(
  tipo: string,
) {
  switch (tipo) {
    case "RemoverDuplicados":
      return "Remover duplicados"

    case "TratarAusentes":
      return "Tratar valores ausentes"

    case "NormalizarTexto":
      return "Normalizar texto"

    case "ConverterTipo":
      return "Converter tipo"

    default:
      return tipo
  }
}

export function DatasetCleaningTab({
  idDataset,
}: {
  idDataset: number
}) {
  const [
    colunas,
    setColunas,
  ] = useState<Coluna[]>([])

  const [
    transformacoes,
    setTransformacoes,
  ] = useState<Transformacao[]>([])

  const [
    tipo,
    setTipo,
  ] =
    useState<TipoTransformacao>(
      "RemoverDuplicados",
    )

  const [
    idColuna,
    setIdColuna,
  ] = useState("")

  const [
    estrategia,
    setEstrategia,
  ] = useState("")

  const [
    valor,
    setValor,
  ] = useState("")

  const [
    novoTipo,
    setNovoTipo,
  ] = useState("Texto")

  const [
    loading,
    setLoading,
  ] = useState(true)

  const [
    saving,
    setSaving,
  ] = useState(false)

  const [
    erro,
    setErro,
  ] =
    useState<string | null>(null)

  const carregar =
    useCallback(async () => {
      try {
        setLoading(true)
        setErro(null)

        const [
          colunasResponse,
          transformacoesResponse,
        ] =
          await Promise.all([
            api.get(
              `/dataset-cleaning/datasets/${idDataset}/colunas`,
            ),

            api.get(
              `/dataset-cleaning/datasets/${idDataset}/transformacoes`,
            ),
          ])

        setColunas(
          lista<Coluna>(
            colunasResponse.data,
          ),
        )

        setTransformacoes(
          lista<Transformacao>(
            transformacoesResponse.data,
          ),
        )
      } catch (error) {
        setErro(
          erroMensagem(
            error,
            "Não foi possível carregar a área de limpeza.",
          ),
        )
      } finally {
        setLoading(false)
      }
    }, [idDataset])

  useEffect(() => {
    void carregar()
  }, [carregar])

  useEffect(() => {
    setIdColuna("")
    setEstrategia("")
    setValor("")

    if (
      tipo === "TratarAusentes"
    ) {
      setEstrategia(
        "Remover linhas",
      )
    }

    if (
      tipo === "NormalizarTexto"
    ) {
      setEstrategia(
        "Remover espaços",
      )
    }

    if (
      tipo === "ConverterTipo"
    ) {
      setNovoTipo("Texto")
    }
  }, [tipo])

  const precisaColuna =
    tipo !== "RemoverDuplicados"

  const pendentes =
    useMemo(
      () =>
        transformacoes.filter(
          (item) =>
            item.estado
              .toLowerCase() !==
            "executada",
        ).length,
      [transformacoes],
    )

  const executadas =
    useMemo(
      () =>
        transformacoes.filter(
          (item) =>
            item.estado
              .toLowerCase() ===
            "executada",
        ).length,
      [transformacoes],
    )

  async function criar(
    event: FormEvent,
  ) {
    event.preventDefault()

    if (
      precisaColuna &&
      !idColuna
    ) {
      setErro(
        "Selecione uma coluna.",
      )
      return
    }

    try {
      setSaving(true)
      setErro(null)

      await api.post(
        `/dataset-cleaning/datasets/${idDataset}/transformacoes`,
        {
          tipo,
          idColuna:
            idColuna
              ? Number(idColuna)
              : null,
          estrategia:
            estrategia || null,
          valor:
            valor || null,
          novoTipo:
            tipo ===
            "ConverterTipo"
              ? novoTipo
              : null,
        },
      )

      await carregar()
    } catch (error) {
      setErro(
        erroMensagem(
          error,
          "Não foi possível criar a transformação.",
        ),
      )
    } finally {
      setSaving(false)
    }
  }

  async function executar(
    item: Transformacao,
  ) {
    try {
      setErro(null)

      await api.put(
        `/dataset-cleaning/datasets/${idDataset}/transformacoes/${item.idTransformacao}/executar`,
      )

      await carregar()
    } catch (error) {
      setErro(
        erroMensagem(
          error,
          "Não foi possível confirmar a transformação.",
        ),
      )
    }
  }

  async function eliminar(
    item: Transformacao,
  ) {
    const confirmar =
      window.confirm(
        `Eliminar "${nomeTipo(
          item.tipo,
        )}" do plano de limpeza?`,
      )

    if (!confirmar) return

    try {
      setErro(null)

      await api.delete(
        `/dataset-cleaning/datasets/${idDataset}/transformacoes/${item.idTransformacao}`,
      )

      await carregar()
    } catch (error) {
      setErro(
        erroMensagem(
          error,
          "Não foi possível eliminar a transformação.",
        ),
      )
    }
  }

  return (
    <div className="df-tab-content">
      <section className="df-cleaning-hero">
        <div>
          <span className="df-kicker">
            DATA CLEANING
          </span>

          <h2>
            Limpeza de dados
          </h2>

          <p>
            Crie um plano auditável de
            transformações antes de aplicar
            alterações físicas ao dataset.
          </p>
        </div>

        <div className="df-cleaning-hero-icon">
          <WandSparkles size={27} />
        </div>
      </section>

      {erro && (
        <div className="df-inline-error">
          <AlertTriangle size={17} />
          {erro}
        </div>
      )}

      <div className="df-cleaning-stats">
        <div>
          <span>
            Transformações
          </span>

          <strong>
            {transformacoes.length}
          </strong>
        </div>

        <div>
          <span>Pendentes</span>

          <strong>
            {pendentes}
          </strong>
        </div>

        <div>
          <span>Executadas</span>

          <strong>
            {executadas}
          </strong>
        </div>

        <div>
          <span>
            Colunas disponíveis
          </span>

          <strong>
            {colunas.length}
          </strong>
        </div>
      </div>

      <section className="df-panel">
        <div className="df-panel-heading">
          <div>
            <span className="df-kicker">
              NOVA TRANSFORMAÇÃO
            </span>

            <h2>
              Adicionar ao plano
            </h2>
          </div>
        </div>

        <form
          className="df-cleaning-form"
          onSubmit={criar}
        >
          <label className="df-clean-field">
            <span>
              Operação
            </span>

            <select
              value={tipo}
              onChange={(event) =>
                setTipo(
                  event.target
                    .value as TipoTransformacao,
                )
              }
            >
              <option value="RemoverDuplicados">
                Remover duplicados
              </option>

              <option value="TratarAusentes">
                Tratar valores ausentes
              </option>

              <option value="NormalizarTexto">
                Normalizar texto
              </option>

              <option value="ConverterTipo">
                Converter tipo
              </option>
            </select>
          </label>

          {precisaColuna && (
            <label className="df-clean-field">
              <span>
                Coluna
              </span>

              <select
                value={idColuna}
                onChange={(event) =>
                  setIdColuna(
                    event.target.value,
                  )
                }
                required
              >
                <option value="">
                  Selecione...
                </option>

                {colunas.map(
                  (coluna) => (
                    <option
                      key={
                        coluna.idColuna
                      }
                      value={
                        coluna.idColuna
                      }
                    >
                      {coluna.nome}
                      {coluna.tipoDetectado
                        ? ` · ${coluna.tipoDetectado}`
                        : ""}
                    </option>
                  ),
                )}
              </select>
            </label>
          )}

          {tipo ===
            "TratarAusentes" && (
            <>
              <label className="df-clean-field">
                <span>
                  Estratégia
                </span>

                <select
                  value={estrategia}
                  onChange={(event) =>
                    setEstrategia(
                      event.target.value,
                    )
                  }
                >
                  <option>
                    Remover linhas
                  </option>

                  <option>
                    Preencher com zero
                  </option>

                  <option>
                    Preencher com média
                  </option>

                  <option>
                    Preencher com mediana
                  </option>

                  <option>
                    Preencher com valor
                  </option>
                </select>
              </label>

              {estrategia ===
                "Preencher com valor" && (
                <label className="df-clean-field">
                  <span>
                    Valor
                  </span>

                  <input
                    value={valor}
                    onChange={(event) =>
                      setValor(
                        event.target.value,
                      )
                    }
                    placeholder="Valor de substituição"
                  />
                </label>
              )}
            </>
          )}

          {tipo ===
            "NormalizarTexto" && (
            <label className="df-clean-field">
              <span>
                Normalização
              </span>

              <select
                value={estrategia}
                onChange={(event) =>
                  setEstrategia(
                    event.target.value,
                  )
                }
              >
                <option>
                  Remover espaços
                </option>

                <option>
                  Minúsculas
                </option>

                <option>
                  Maiúsculas
                </option>

                <option>
                  Capitalizar
                </option>
              </select>
            </label>
          )}

          {tipo ===
            "ConverterTipo" && (
            <label className="df-clean-field">
              <span>
                Novo tipo
              </span>

              <select
                value={novoTipo}
                onChange={(event) =>
                  setNovoTipo(
                    event.target.value,
                  )
                }
              >
                <option>Texto</option>
                <option>Inteiro</option>
                <option>Decimal</option>
                <option>Data</option>
                <option>Booleano</option>
              </select>
            </label>
          )}

          <div className="df-cleaning-submit">
            <button
              type="submit"
              className="df-primary-button"
              disabled={
                saving ||
                loading
              }
            >
              {saving ? (
                <LoaderCircle
                  size={17}
                  className="spin"
                />
              ) : (
                <Plus size={17} />
              )}

              {saving
                ? "A adicionar..."
                : "Adicionar transformação"}
            </button>
          </div>
        </form>
      </section>

      <section className="df-panel">
        <div className="df-panel-heading">
          <div>
            <span className="df-kicker">
              PIPELINE
            </span>

            <h2>
              Plano de limpeza
            </h2>
          </div>

          <button
            type="button"
            className="df-icon-button"
            onClick={() =>
              void carregar()
            }
            title="Atualizar"
          >
            <RefreshCw size={17} />
          </button>
        </div>

        {loading ? (
          <div className="df-cleaning-loading">
            <LoaderCircle
              size={22}
              className="spin"
            />

            A carregar transformações...
          </div>
        ) : transformacoes.length ===
          0 ? (
          <div className="df-cleaning-empty">
            <Eraser size={29} />

            <strong>
              Plano de limpeza vazio
            </strong>

            <p>
              Adicione uma transformação
              para começar.
            </p>
          </div>
        ) : (
          <div className="df-cleaning-list">
            {transformacoes.map(
              (item, index) => {
                const executada =
                  item.estado
                    .toLowerCase() ===
                  "executada"

                return (
                  <article
                    key={
                      item.idTransformacao
                    }
                    className="df-cleaning-item"
                  >
                    <div className="df-cleaning-step">
                      {index + 1}
                    </div>

                    <div className="df-cleaning-item-main">
                      <div className="df-cleaning-item-title">
                        <div>
                          {item.tipo ===
                          "ConverterTipo" ? (
                            <Type size={18} />
                          ) : (
                            <Sparkles
                              size={18}
                            />
                          )}

                          <div>
                            <strong>
                              {nomeTipo(
                                item.tipo,
                              )}
                            </strong>

                            <span>
                              {item.coluna ??
                                "Dataset completo"}
                            </span>
                          </div>
                        </div>

                        <span
                          className={
                            executada
                              ? "df-clean-status done"
                              : "df-clean-status pending"
                          }
                        >
                          {executada ? (
                            <CheckCircle2
                              size={13}
                            />
                          ) : (
                            <Clock3
                              size={13}
                            />
                          )}

                          {item.estado}
                        </span>
                      </div>

                      {item.descricao && (
                        <p>
                          {
                            item.descricao
                          }
                        </p>
                      )}

                      <div className="df-cleaning-meta">
                        <span>
                          Criada{" "}
                          {dataHora(
                            item.dataCriacao,
                          )}
                        </span>

                        {item.dataExecucao && (
                          <span>
                            Executada{" "}
                            {dataHora(
                              item.dataExecucao,
                            )}
                          </span>
                        )}
                      </div>
                    </div>

                    <div className="df-cleaning-actions">
                      {!executada && (
                        <button
                          type="button"
                          className="df-clean-execute"
                          onClick={() =>
                            void executar(
                              item,
                            )
                          }
                          title="Confirmar transformação"
                        >
                          <Check
                            size={16}
                          />
                          Executar
                        </button>
                      )}

                      <button
                        type="button"
                        className="df-danger-icon"
                        onClick={() =>
                          void eliminar(
                            item,
                          )
                        }
                        title="Eliminar"
                      >
                        <Trash2
                          size={16}
                        />
                      </button>
                    </div>
                  </article>
                )
              },
            )}
          </div>
        )}
      </section>

      <div className="df-cleaning-note">
        <AlertTriangle size={17} />

        <div>
          <strong>
            Dataset original protegido
          </strong>

          <p>
            Nesta fase, “Executar” confirma
            a transformação no plano e no
            histórico. O ficheiro CSV original
            não é reescrito até o motor físico
            de transformações ser implementado.
          </p>
        </div>
      </div>
    </div>
  )
}
