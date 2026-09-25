using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Projeto
{
    public int IdProjeto { get; set; }

    public int IdUtilizador { get; set; }

    public string Nome { get; set; } = null!;

    public string? Descricao { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime? DataAtualizacao { get; set; }

    public virtual ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();

    public virtual ICollection<Historico> Historicos { get; set; } = new List<Historico>();

    public virtual Utilizadore IdUtilizadorNavigation { get; set; } = null!;
}
