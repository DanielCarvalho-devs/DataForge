namespace DataForge.Application.DTOs.Datasets;

public class ResumoColunaDto
{
    public int IdDataset { get; set; }

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

    public bool TemEstatisticasNumericas { get; set; }

    public EstatisticasResumoColunaDto? Estatisticas { get; set; }
}

public class EstatisticasResumoColunaDto
{
    public decimal? Minimo { get; set; }

    public decimal? Maximo { get; set; }

    public decimal? Media { get; set; }

    public decimal? Mediana { get; set; }

    public decimal? DesvioPadrao { get; set; }

    public decimal? P25 { get; set; }

    public decimal? P50 { get; set; }

    public decimal? P75 { get; set; }

    public decimal? P90 { get; set; }

    public decimal? P99 { get; set; }
}
