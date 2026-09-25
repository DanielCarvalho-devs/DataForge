namespace DataForge.Application.DTOs.Datasets;

public class DatasetDto
{
    public int IdDataset { get; set; }

    public int IdProjeto { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string NomeOriginal { get; set; } = string.Empty;

    public string TipoArquivo { get; set; } = string.Empty;

    public long? TamanhoBytes { get; set; }

    public long TotalRegistos { get; set; }

    public int TotalColunas { get; set; }

    public decimal? QualityScore { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime DataImportacao { get; set; }

    public DateTime? DataProcessamento { get; set; }
}
