namespace DataForge.Application.DTOs.Datasets;

public class OutliersDatasetDto
{
    public int IdDataset { get; set; }

    public long TotalRegistos { get; set; }

    public int TotalColunasAnalisadas { get; set; }

    public long TotalOutliers { get; set; }

    public decimal PercentualOutliers { get; set; }

    public IEnumerable<OutlierDto> Outliers { get; set; }
        = Enumerable.Empty<OutlierDto>();
}

public class OutlierDto
{
    public long IdAnomalia { get; set; }

    public int IdColuna { get; set; }

    public string NomeColuna { get; set; } = string.Empty;

    public long? NumeroLinha { get; set; }

    public string? ValorOriginal { get; set; }

    public decimal? ValorNumerico { get; set; }

    public decimal P25 { get; set; }

    public decimal P75 { get; set; }

    public decimal Iqr { get; set; }

    public decimal LimiteInferior { get; set; }

    public decimal LimiteSuperior { get; set; }

    public decimal? Score { get; set; }

    public string? Descricao { get; set; }

    public bool Ignorada { get; set; }

    public DateTime DataDeteccao { get; set; }
}
