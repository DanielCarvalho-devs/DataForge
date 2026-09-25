using System.ComponentModel.DataAnnotations;

namespace DataForge.Application.DTOs.Projetos;

public class CriarProjetoDto
{
    [Required(ErrorMessage = "O nome do projeto é obrigatório.")]
    [StringLength(150, ErrorMessage = "O nome pode ter no máximo 150 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "A descrição pode ter no máximo 1000 caracteres.")]
    public string? Descricao { get; set; }
}
