namespace DataForge.Application.DTOs.Datasets;

public class ProblemaQualidadeDto
{
    public long IdProblema { get; set; }

    public int IdDataset { get; set; }

    public int? IdColuna { get; set; }

    public string? NomeColuna { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Severidade { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public long Quantidade { get; set; }

    public bool Resolvido { get; set; }

    public DateTime DataDeteccao { get; set; }

    public DateTime? DataResolucao { get; set; }
}
