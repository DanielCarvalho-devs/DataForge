using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Historico
{
    public long IdHistorico { get; set; }

    public int? IdUtilizador { get; set; }

    public int? IdProjeto { get; set; }

    public int? IdDataset { get; set; }

    public string Acao { get; set; } = null!;

    public string? Entidade { get; set; }

    public long? IdRegisto { get; set; }

    public string? Descricao { get; set; }

    public DateTime DataAcao { get; set; }

    public virtual Dataset? IdDatasetNavigation { get; set; }

    public virtual Projeto? IdProjetoNavigation { get; set; }

    public virtual Utilizadore? IdUtilizadorNavigation { get; set; }
}
