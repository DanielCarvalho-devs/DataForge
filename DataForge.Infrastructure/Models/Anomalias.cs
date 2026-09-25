using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Anomalias
{
    public long IdAnomalia { get; set; }

    public int IdDataset { get; set; }

    public int? IdColuna { get; set; }

    public long? NumeroLinha { get; set; }

    public string? ValorOriginal { get; set; }

    public string Tipo { get; set; } = null!;

    public decimal? Score { get; set; }

    public string? Descricao { get; set; }

    public bool Ignorada { get; set; }

    public DateTime DataDeteccao { get; set; }

    public virtual DatasetColuna? IdColunaNavigation { get; set; }

    public virtual Dataset IdDatasetNavigation { get; set; } = null!;
}
