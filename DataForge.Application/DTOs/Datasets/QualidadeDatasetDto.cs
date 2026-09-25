namespace DataForge.Application.DTOs.Datasets;

public class QualidadeDatasetDto
{
    public int IdDataset { get; set; }

    public decimal QualityScore { get; set; }

    public long TotalCelulas { get; set; }

    public long ValoresValidos { get; set; }

    public long ValoresAusentes { get; set; }

    public decimal PercentualCompletude { get; set; }

    public decimal PercentualUnicidade { get; set; }

    public int TotalProblemas { get; set; }

    public int ProblemasCriticos { get; set; }

    public int ProblemasErro { get; set; }

    public int ProblemasAviso { get; set; }

    public int ProblemasInformacao { get; set; }

    public IEnumerable<ProblemaQualidadeDto> Problemas { get; set; }
        = Enumerable.Empty<ProblemaQualidadeDto>();
}

