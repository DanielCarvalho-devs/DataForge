import {
  BarChart3,
  Database,
  FileBarChart,
  History,
  ShieldCheck,
  Sparkles,
  Wand2,
} from "lucide-react"

import { Link } from "react-router-dom"

type Tool =
  | "datasets"
  | "exploracao"
  | "qualidade"
  | "limpeza"
  | "analises"
  | "relatorios"
  | "historico"

const config = {
  datasets: {
    title: "Datasets",
    text: "Aceda aos datasets atraves dos projetos e abra o respetivo workspace.",
    icon: Database,
  },
  exploracao: {
    title: "Exploracao",
    text: "Explore distribuicoes, perfis e estatisticas diretamente no Dataset Workspace.",
    icon: BarChart3,
  },
  qualidade: {
    title: "Qualidade",
    text: "Analise valores ausentes, duplicados, outliers e o Data Quality Score.",
    icon: ShieldCheck,
  },
  limpeza: {
    title: "Limpeza",
    text: "Area preparada para as transformacoes e operacoes de limpeza.",
    icon: Wand2,
  },
  analises: {
    title: "Analises",
    text: "Area preparada para analises persistidas e novos motores analiticos.",
    icon: Sparkles,
  },
  relatorios: {
    title: "Relatorios",
    text: "Area preparada para relatorios e exportacao.",
    icon: FileBarChart,
  },
  historico: {
    title: "Historico",
    text: "Area preparada para eventos, transformacoes e auditoria.",
    icon: History,
  },
} satisfies Record<
  Tool,
  {
    title: string
    text: string
    icon: typeof Database
  }
>

export function ToolPage({
  tool,
}: {
  tool: Tool
}) {
  const item = config[tool]
  const Icon = item.icon

  return (
    <div className="df-workspace-page">
      <header className="df-workspace-header">
        <div>
          <span className="df-kicker">
            DATA TOOLS
          </span>

          <h1>{item.title}</h1>

          <p>{item.text}</p>
        </div>
      </header>

      <section className="df-tool-landing">
        <span>
          <Icon size={30} />
        </span>

        <h2>{item.title}</h2>

        <p>
          As funcionalidades operacionais desta area
          sao executadas dentro do workspace de cada
          dataset para manter contexto e rastreabilidade.
        </p>

        <Link to="/projetos">
          Abrir projetos
        </Link>
      </section>
    </div>
  )
}
