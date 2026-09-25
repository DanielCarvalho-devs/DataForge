using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class DatasetColuna
{
    public int IdColuna { get; set; }

    public int IdDataset { get; set; }

    public string Nome { get; set; } = null!;

    public string? TipoOriginal { get; set; }

    public string? TipoDetectado { get; set; }

    public int Posicao { get; set; }

    public long TotalValores { get; set; }

    public long ValoresValidos { get; set; }

    public long ValoresAusentes { get; set; }

    public long ValoresUnicos { get; set; }

    public decimal? PercentualValido { get; set; }

    public virtual ICollection<Anomalias> Anomaliases { get; set; } = new List<Anomalias>();

    public virtual ICollection<EstatisticasColuna> EstatisticasColunas { get; set; } = new List<EstatisticasColuna>();

    public virtual Dataset IdDatasetNavigation { get; set; } = null!;

    public virtual ICollection<ProblemasQualidade> ProblemasQualidades { get; set; } = new List<ProblemasQualidade>();

    public virtual ICollection<Transformaco> Transformacos { get; set; } = new List<Transformaco>();
}
