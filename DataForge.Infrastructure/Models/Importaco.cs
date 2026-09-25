using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Importaco
{
    public long IdImportacao { get; set; }

    public int IdDataset { get; set; }

    public int IdUtilizador { get; set; }

    public string Estado { get; set; } = null!;

    public int Percentual { get; set; }

    public long RegistosProcessados { get; set; }

    public string? MensagemErro { get; set; }

    public DateTime? InicioProcessamento { get; set; }

    public DateTime? FimProcessamento { get; set; }

    public DateTime DataCriacao { get; set; }

    public virtual Dataset IdDatasetNavigation { get; set; } = null!;

    public virtual Utilizadore IdUtilizadorNavigation { get; set; } = null!;
}
