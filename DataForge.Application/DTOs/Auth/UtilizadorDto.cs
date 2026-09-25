namespace DataForge.Application.DTOs.Auth;

public class UtilizadorDto
{
    public int IdUtilizador { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;

    public bool Ativo { get; set; }

    public DateTime DataCriacao { get; set; }

    public DateTime? UltimoAcesso { get; set; }
}
