namespace DataForge.Application.DTOs.Datasets;

public class DatasetColunaDto
{
    public int IdColuna { get; set; }

    public int IdDataset { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? TipoOriginal { get; set; }

    public string? TipoDetectado { get; set; }

    public int Posicao { get; set; }

    public long TotalValores { get; set; }

    public long ValoresValidos { get; set; }

    public long ValoresAusentes { get; set; }

    public long ValoresUnicos { get; set; }

    public decimal? PercentualValido { get; set; }
}
