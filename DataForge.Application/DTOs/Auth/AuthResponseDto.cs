namespace DataForge.Application.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }

    public UtilizadorDto Utilizador { get; set; } = new();
}
