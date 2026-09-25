namespace DataForge.Application.DTOs.Datasets;

public class DatasetPreviewDto
{
    public int IdDataset { get; set; }

    public long TotalRegistos { get; set; }

    public int TotalColunas { get; set; }

    public int Pagina { get; set; }

    public int TamanhoPagina { get; set; }

    public long TotalPaginas { get; set; }

    public bool TemPaginaAnterior { get; set; }

    public bool TemProximaPagina { get; set; }

    public IEnumerable<DatasetPreviewColunaDto> Colunas { get; set; }
        = Enumerable.Empty<DatasetPreviewColunaDto>();

    public IEnumerable<Dictionary<string, string?>> Registos { get; set; }
        = Enumerable.Empty<Dictionary<string, string?>>();
}

public class DatasetPreviewColunaDto
{
    public int IdColuna { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? TipoDetectado { get; set; }

    public int Posicao { get; set; }
}
