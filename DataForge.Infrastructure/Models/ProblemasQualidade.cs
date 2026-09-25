using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class ProblemasQualidade
{
    public long IdProblema { get; set; }

    public int IdDataset { get; set; }

    public int? IdColuna { get; set; }

    public string Tipo { get; set; } = null!;

    public string Severidade { get; set; } = null!;

    public string Descricao { get; set; } = null!;

    public long Quantidade { get; set; }

    public bool Resolvido { get; set; }

    public DateTime DataDeteccao { get; set; }

    public DateTime? DataResolucao { get; set; }

    public virtual DatasetColuna? IdColunaNavigation { get; set; }

    public virtual Dataset IdDatasetNavigation { get; set; } = null!;
}
