namespace DataForge.Application.DTOs.Datasets;

public class DistribuicaoColunaDto
{
    public int IdDataset { get; set; }

    public int IdColuna { get; set; }

    public string NomeColuna { get; set; } = string.Empty;

    public string? TipoDetectado { get; set; }

    public long TotalValores { get; set; }

    public long ValoresValidos { get; set; }

    public long ValoresAusentes { get; set; }

    public long ValoresDistintos { get; set; }

    public int TopSolicitado { get; set; }

    public bool DistribuicaoTruncada { get; set; }

    public IEnumerable<ItemDistribuicaoDto> Distribuicao { get; set; }
        = Enumerable.Empty<ItemDistribuicaoDto>();
}

public class ItemDistribuicaoDto
{
    public string Valor { get; set; } = string.Empty;

    public long Quantidade { get; set; }

    public decimal Percentual { get; set; }
}
