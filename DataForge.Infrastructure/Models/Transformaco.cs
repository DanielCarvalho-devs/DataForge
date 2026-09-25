using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Transformaco
{
    public long IdTransformacao { get; set; }

    public int IdDataset { get; set; }

    public int? IdColuna { get; set; }

    public int IdUtilizador { get; set; }

    public string Tipo { get; set; } = null!;

    public string? Descricao { get; set; }

    public string? ParametrosJson { get; set; }

    public long? RegistosAfetados { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime DataCriacao { get; set; }

    public DateTime? DataExecucao { get; set; }

    public virtual DatasetColuna? IdColunaNavigation { get; set; }

    public virtual Dataset IdDatasetNavigation { get; set; } = null!;

    public virtual Utilizadore IdUtilizadorNavigation { get; set; } = null!;
}
