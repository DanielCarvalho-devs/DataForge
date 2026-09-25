namespace DataForge.Application.DTOs.Datasets;

public class EstatisticaColunaDto
{
    public long IdEstatistica { get; set; }

    public int IdColuna { get; set; }

    public string NomeColuna { get; set; } = string.Empty;

    public string? TipoDetectado { get; set; }

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

    public DateTime DataCalculo { get; set; }
}
