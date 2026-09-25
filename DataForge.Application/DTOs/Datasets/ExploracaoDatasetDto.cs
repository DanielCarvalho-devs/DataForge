namespace DataForge.Application.DTOs.Datasets;

public class ExploracaoDatasetDto
{
    public int IdDataset { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string NomeOriginal { get; set; } = string.Empty;

    public string TipoArquivo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public long TotalRegistos { get; set; }

    public int TotalColunas { get; set; }

    public long? TamanhoBytes { get; set; }

    public decimal? QualityScore { get; set; }

    public DateTime DataImportacao { get; set; }

    public DateTime? DataProcessamento { get; set; }

    public ResumoQualidadeExploracaoDto Qualidade { get; set; } = new();

    public IEnumerable<ColunaExploracaoDto> Colunas { get; set; }
        = Enumerable.Empty<ColunaExploracaoDto>();
}

public class ResumoQualidadeExploracaoDto
{
    public int TotalProblemas { get; set; }

    public int ProblemasCriticos { get; set; }

    public int ProblemasErro { get; set; }

    public int ProblemasAviso { get; set; }

    public int ProblemasInformacao { get; set; }

    public int TotalDuplicados { get; set; }

    public int TotalOutliers { get; set; }

    public int TotalValoresAusentes { get; set; }
}

public class ColunaExploracaoDto
{
    public int IdColuna { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? TipoOriginal { get; set; }

    public string? TipoDetectado { get; set; }

    public string PapelSemantico { get; set; } = string.Empty;

    public int Posicao { get; set; }

    public long TotalValores { get; set; }

    public long ValoresValidos { get; set; }

    public long ValoresAusentes { get; set; }

    public long ValoresUnicos { get; set; }

    public decimal PercentualValido { get; set; }

    public decimal PercentualAusente { get; set; }

    public decimal PercentualUnico { get; set; }

    public int TotalProblemas { get; set; }

    public int TotalOutliers { get; set; }

    public bool TemEstatisticasNumericas { get; set; }
}
