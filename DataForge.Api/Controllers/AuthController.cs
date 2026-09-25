using DataForge.Application.DTOs.Auth;
using DataForge.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DataForge.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registar")]
    public async Task<ActionResult<AuthResponseDto>> Registar(
        [FromBody] RegistarUtilizadorDto dto)
    {
        try
        {
            var resultado = await _authService.RegistarAsync(dto);

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto)
    {
        var resultado = await _authService.LoginAsync(dto);

        if (resultado is null)
        {
            return Unauthorized(new
            {
                mensagem = "Email ou senha inválidos."
            });
        }

        return Ok(resultado);
    }
}
