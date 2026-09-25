export type Projeto = {
  idProjeto: number
  idUtilizador?: number
  nome: string
  descricao?: string | null
  ativo?: boolean
  dataCriacao?: string
  dataAtualizacao?: string | null
}

export type Dataset = {
  idDataset: number
  idProjeto: number
  nome: string
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

export type Coluna = {
  idColuna: number
  idDataset?: number
  nome: string
  tipoDado?: string
  tipo?: string
  permiteNulos?: boolean
  totalNulos?: number
  valoresNulos?: number
  valoresDistintos?: number
  papelSemantico?: string
  semanticRole?: string
  ordem?: number
}

export type ProblemaQualidade = {
  idProblemaQualidade?: number
  tipo?: string
  descricao?: string
  severidade?: string
  coluna?: string
  quantidade?: number
}

export function normalizarLista<T>(valor: unknown): T[] {
  if (Array.isArray(valor)) return valor as T[]

  if (valor && typeof valor === "object") {
    const objeto = valor as Record<string, unknown>

    const candidatos = [
      objeto.items,
      objeto.data,
      objeto.resultados,
      objeto.projetos,
      objeto.datasets,
      objeto.colunas,
      objeto.problemas,
      objeto.estatisticas,
      objeto.registos,
      objeto.rows,
    ]

    for (const candidato of candidatos) {
      if (Array.isArray(candidato)) return candidato as T[]
    }
  }

  return []
}

export function numero(valor?: number | null) {
  return new Intl.NumberFormat("pt-PT").format(valor ?? 0)
}

export function percentagem(valor?: number | null) {
  if (valor === null || valor === undefined) return "—"

  return `${new Intl.NumberFormat("pt-PT", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(valor)}%`
}

export function data(valor?: string | null) {
  if (!valor) return "—"

  const d = new Date(valor)

  if (Number.isNaN(d.getTime())) return "—"

  return new Intl.DateTimeFormat("pt-PT", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  }).format(d)
}

export function mensagemErro(
  erro: unknown,
  fallback = "Ocorreu um erro.",
) {
  if (erro && typeof erro === "object" && "response" in erro) {
    const response = (
      erro as {
        response?: {
          data?: {
            mensagem?: string
            message?: string
          }
        }
      }
    ).response

    return (
      response?.data?.mensagem ??
      response?.data?.message ??
      fallback
    )
  }

  return fallback
}
