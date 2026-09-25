using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Analise
{
    public long IdAnalise { get; set; }

    public int IdDataset { get; set; }

    public int IdUtilizador { get; set; }

    public string Nome { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string? ConfiguracaoJson { get; set; }

    public string? ResultadoJson { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime? DataAtualizacao { get; set; }

    public virtual Dataset IdDatasetNavigation { get; set; } = null!;

    public virtual Utilizadore IdUtilizadorNavigation { get; set; } = null!;
}
