namespace DataForge.Application.DTOs.Projetos;

public class ProjetoDto
{
    public int IdProjeto { get; set; }

    public int IdUtilizador { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public bool Ativo { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime? DataAtualizacao { get; set; }
}
