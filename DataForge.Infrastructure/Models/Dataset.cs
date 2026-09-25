using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Dataset
{
    public int IdDataset { get; set; }

    public int IdProjeto { get; set; }

    public string Nome { get; set; } = null!;

    public string NomeOriginal { get; set; } = null!;

    public string TipoArquivo { get; set; } = null!;

    public string? CaminhoArquivo { get; set; }

    public long? TamanhoBytes { get; set; }

    public long TotalRegistos { get; set; }

    public int TotalColunas { get; set; }

    public decimal? QualityScore { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime DataImportacao { get; set; }

    public DateTime? DataProcessamento { get; set; }

    public virtual ICollection<Analise> Analises { get; set; } = new List<Analise>();

    public virtual ICollection<Anomalias> Anomaliases { get; set; } = new List<Anomalias>();

    public virtual ICollection<DatasetColuna> DatasetColunas { get; set; } = new List<DatasetColuna>();

    public virtual ICollection<Historico> Historicos { get; set; } = new List<Historico>();

    public virtual Projeto IdProjetoNavigation { get; set; } = null!;

    public virtual ICollection<Importaco> Importacos { get; set; } = new List<Importaco>();

    public virtual ICollection<ProblemasQualidade> ProblemasQualidades { get; set; } = new List<ProblemasQualidade>();

    public virtual ICollection<Transformaco> Transformacos { get; set; } = new List<Transformaco>();
}
