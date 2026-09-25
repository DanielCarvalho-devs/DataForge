namespace DataForge.Application.DTOs.Datasets;

public class DuplicadosDatasetDto
{
    public int IdDataset { get; set; }

    public long TotalRegistos { get; set; }

    public long LinhasDuplicadas { get; set; }

    public decimal PercentualDuplicados { get; set; }

    public IEnumerable<AnomaliaDuplicadoDto> Duplicados { get; set; }
        = Enumerable.Empty<AnomaliaDuplicadoDto>();
}

public class AnomaliaDuplicadoDto
{
    public long IdAnomalia { get; set; }

    public long? NumeroLinha { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string? ValorOriginal { get; set; }

    public string? Descricao { get; set; }

    public decimal? Score { get; set; }

    public bool Ignorada { get; set; }

    public DateTime DataDeteccao { get; set; }
}
