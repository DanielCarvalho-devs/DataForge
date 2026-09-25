using System;
using System.Collections.Generic;

namespace DataForge.Infrastructure.Models;

public partial class Utilizadore
{
    public int IdUtilizador { get; set; }

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Perfil { get; set; } = null!;

    public bool Ativo { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime? UltimoAcesso { get; set; }

    public string? FotoPerfil { get; set; }

    public virtual ICollection<Analise> Analises { get; set; } = new List<Analise>();

    public virtual ICollection<Historico> Historicos { get; set; } = new List<Historico>();

    public virtual ICollection<Importaco> Importacos { get; set; } = new List<Importaco>();

    public virtual ICollection<Projeto> Projetos { get; set; } = new List<Projeto>();

    public virtual ICollection<Transformaco> Transformacos { get; set; } = new List<Transformaco>();
}

