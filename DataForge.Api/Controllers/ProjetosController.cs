using System.Security.Claims;
using DataForge.Application.DTOs.Projetos;
using DataForge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/projetos")]
public class ProjetosController : ControllerBase
{
    private readonly IProjetoService _projetoService;

    public ProjetosController(IProjetoService projetoService)
    {
        _projetoService = projetoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjetoDto>>> ObterTodos()
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var projetos =
            await _projetoService.ObterTodosAsync(
                idUtilizador.Value
            );

        return Ok(projetos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjetoDto>> ObterPorId(int id)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var projeto =
            await _projetoService.ObterPorIdAsync(
                id,
                idUtilizador.Value
            );

        if (projeto is null)
        {
            return NotFound(new
            {
                mensagem = "Projeto não encontrado."
            });
        }

        return Ok(projeto);
    }

    [HttpPost]
    public async Task<ActionResult<ProjetoDto>> Criar(
        [FromBody] CriarProjetoDto dto)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        try
        {
            var projeto =
                await _projetoService.CriarAsync(
                    idUtilizador.Value,
                    dto
                );

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = projeto.IdProjeto },
                projeto
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensagem = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjetoDto>> Atualizar(
        int id,
        [FromBody] AtualizarProjetoDto dto)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var projeto =
            await _projetoService.AtualizarAsync(
                id,
                idUtilizador.Value,
                dto
            );

        if (projeto is null)
        {
            return NotFound(new
            {
                mensagem = "Projeto não encontrado."
            });
        }

        return Ok(projeto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var idUtilizador = ObterIdUtilizador();

        if (idUtilizador is null)
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var eliminado =
            await _projetoService.EliminarAsync(
                id,
                idUtilizador.Value
            );

        if (!eliminado)
        {
            return NotFound(new
            {
                mensagem = "Projeto não encontrado."
            });
        }

        return Ok(new
        {
            mensagem = "Projeto desativado com sucesso."
        });
    }

    private int? ObterIdUtilizador()
    {
        var claim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (int.TryParse(claim, out var idUtilizador))
        {
            return idUtilizador;
        }

        return null;
    }
}
