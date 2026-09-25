using System.Security.Claims;
using System.Text.Json;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/dataset-cleaning")]
public class DatasetCleaningController : ControllerBase
{
    private readonly DataForgeDbContext _db;

    public DatasetCleaningController(DataForgeDbContext db)
    {
        _db = db;
    }

    private int ObterIdUtilizador()
    {
        var claim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

        if (!int.TryParse(claim, out var id))
        {
            throw new UnauthorizedAccessException(
                "Utilizador não identificado."
            );
        }

        return id;
    }

    private async Task<Dataset?> ObterDatasetAsync(
        int idDataset,
        int idUtilizador)
    {
        return await _db.Datasets
            .Include(d => d.IdProjetoNavigation)
            .FirstOrDefaultAsync(d =>
                d.IdDataset == idDataset &&
                d.IdProjetoNavigation.IdUtilizador ==
                    idUtilizador &&
                d.IdProjetoNavigation.Ativo
            );
    }

    private async Task RegistarHistoricoAsync(
        Dataset dataset,
        int idUtilizador,
        string acao,
        long? idRegisto,
        string descricao)
    {
        _db.Historicos.Add(
            new Historico
            {
                IdUtilizador = idUtilizador,
                IdProjeto = dataset.IdProjeto,
                IdDataset = dataset.IdDataset,
                Acao = acao,
                Entidade = "Transformacao",
                IdRegisto = idRegisto,
                Descricao = descricao,
                DataAcao = DateTime.Now
            }
        );

        await _db.SaveChangesAsync();
    }

    // ========================================================
    // COLUNAS DISPONIVEIS PARA LIMPEZA
    // ========================================================

    [HttpGet("datasets/{idDataset:int}/colunas")]
    public async Task<IActionResult> ObterColunas(
        int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        var dataset =
            await ObterDatasetAsync(
                idDataset,
                idUtilizador
            );

        if (dataset is null)
        {
            return NotFound(
                "Dataset não encontrado."
            );
        }

        var colunas =
            await _db.DatasetColunas
                .AsNoTracking()
                .Where(c =>
                    c.IdDataset == idDataset
                )
                .OrderBy(c => c.Posicao)
                .Select(c => new
                {
                    c.IdColuna,
                    c.Nome,
                    c.TipoDetectado,
                    c.TipoOriginal,
                    c.PercentualValido
                })
                .ToListAsync();

        return Ok(colunas);
    }

    // ========================================================
    // LISTAR TRANSFORMACOES
    // ========================================================

    [HttpGet("datasets/{idDataset:int}/transformacoes")]
    public async Task<IActionResult> ObterTransformacoes(
        int idDataset)
    {
        var idUtilizador =
            ObterIdUtilizador();

        var dataset =
            await ObterDatasetAsync(
                idDataset,
                idUtilizador
            );

        if (dataset is null)
        {
            return NotFound(
                "Dataset não encontrado."
            );
        }

        var transformacoes =
            await _db.Transformacoes
                .AsNoTracking()
                .Where(t =>
                    t.IdDataset == idDataset &&
                    t.IdUtilizador == idUtilizador
                )
                .OrderByDescending(t =>
                    t.DataCriacao
                )
                .Select(t => new
                {
                    t.IdTransformacao,
                    t.IdDataset,
                    t.IdColuna,
                    Coluna =
                        t.IdColunaNavigation != null
                            ? t.IdColunaNavigation.Nome
                            : null,
                    t.Tipo,
                    t.Descricao,
                    t.ParametrosJson,
                    t.RegistosAfetados,
                    t.Estado,
                    t.DataCriacao,
                    t.DataExecucao
                })
                .ToListAsync();

        return Ok(transformacoes);
    }

    // ========================================================
    // CRIAR TRANSFORMACAO
    // ========================================================

    [HttpPost("datasets/{idDataset:int}/transformacoes")]
    public async Task<IActionResult> CriarTransformacao(
        int idDataset,
        [FromBody] CriarTransformacaoRequest request)
    {
        var idUtilizador =
            ObterIdUtilizador();

        var dataset =
            await ObterDatasetAsync(
                idDataset,
                idUtilizador
            );

        if (dataset is null)
        {
            return NotFound(
                "Dataset não encontrado."
            );
        }

        var tiposPermitidos =
            new[]
            {
                "RemoverDuplicados",
                "TratarAusentes",
                "NormalizarTexto",
                "ConverterTipo"
            };

        if (
            string.IsNullOrWhiteSpace(request.Tipo) ||
            !tiposPermitidos.Contains(
                request.Tipo,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            return BadRequest(
                "Tipo de transformação inválido."
            );
        }

        var tipo =
            tiposPermitidos.First(x =>
                x.Equals(
                    request.Tipo,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        DatasetColuna? coluna = null;

        if (request.IdColuna.HasValue)
        {
            coluna =
                await _db.DatasetColunas
                    .FirstOrDefaultAsync(c =>
                        c.IdColuna ==
                            request.IdColuna.Value &&
                        c.IdDataset == idDataset
                    );

            if (coluna is null)
            {
                return BadRequest(
                    "A coluna selecionada não pertence ao dataset."
                );
            }
        }

        if (
            tipo != "RemoverDuplicados" &&
            coluna is null
        )
        {
            return BadRequest(
                "Selecione uma coluna para esta transformação."
            );
        }

        var parametros =
            new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(request.Estrategia))
        {
            parametros["estrategia"] =
                request.Estrategia.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Valor))
        {
            parametros["valor"] =
                request.Valor.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.NovoTipo))
        {
            parametros["novoTipo"] =
                request.NovoTipo.Trim();
        }

        var descricao =
            CriarDescricao(
                tipo,
                coluna?.Nome,
                request
            );

        var transformacao =
            new Transformaco
            {
                IdDataset = idDataset,
                IdColuna = coluna?.IdColuna,
                IdUtilizador = idUtilizador,
                Tipo = tipo,
                Descricao = descricao,
                ParametrosJson =
                    parametros.Count > 0
                        ? JsonSerializer.Serialize(parametros)
                        : null,
                RegistosAfetados = null,
                Estado = "Pendente",
                DataCriacao = DateTime.Now,
                DataExecucao = null
            };

        _db.Transformacoes.Add(
            transformacao
        );

        await _db.SaveChangesAsync();

        await RegistarHistoricoAsync(
            dataset,
            idUtilizador,
            "Transformação criada",
            transformacao.IdTransformacao,
            descricao
        );

        return Ok(
            new
            {
                transformacao.IdTransformacao,
                transformacao.IdDataset,
                transformacao.IdColuna,
                Coluna = coluna?.Nome,
                transformacao.Tipo,
                transformacao.Descricao,
                transformacao.ParametrosJson,
                transformacao.RegistosAfetados,
                transformacao.Estado,
                transformacao.DataCriacao,
                transformacao.DataExecucao
            }
        );
    }

    // ========================================================
    // MARCAR COMO EXECUTADA
    //
    // IMPORTANTE:
    // Nesta fase isto confirma a transformação no plano e
    // regista a auditoria. Não reescreve o CSV original.
    // ========================================================

    [HttpPut(
        "datasets/{idDataset:int}/transformacoes/{idTransformacao:long}/executar"
    )]
    public async Task<IActionResult> ExecutarTransformacao(
        int idDataset,
        long idTransformacao)
    {
        var idUtilizador =
            ObterIdUtilizador();

        var dataset =
            await ObterDatasetAsync(
                idDataset,
                idUtilizador
            );

        if (dataset is null)
        {
            return NotFound(
                "Dataset não encontrado."
            );
        }

        var transformacao =
            await _db.Transformacoes
                .FirstOrDefaultAsync(t =>
                    t.IdTransformacao ==
                        idTransformacao &&
                    t.IdDataset == idDataset &&
                    t.IdUtilizador ==
                        idUtilizador
                );

        if (transformacao is null)
        {
            return NotFound(
                "Transformação não encontrada."
            );
        }

        if (
            transformacao.Estado.Equals(
                "Executada",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return BadRequest(
                "Esta transformação já foi executada."
            );
        }

        transformacao.Estado =
            "Executada";

        transformacao.DataExecucao =
            DateTime.Now;

        /*
         * O motor físico de transformação do CSV será
         * implementado separadamente.
         *
         * Não indicamos RegistosAfetados sem efetivamente
         * processar o ficheiro.
         */
        transformacao.RegistosAfetados =
            null;

        await _db.SaveChangesAsync();

        await RegistarHistoricoAsync(
            dataset,
            idUtilizador,
            "Transformação executada",
            transformacao.IdTransformacao,
            transformacao.Descricao ??
                $"Transformação {transformacao.Tipo} executada."
        );

        return Ok(
            new
            {
                transformacao.IdTransformacao,
                transformacao.Estado,
                transformacao.RegistosAfetados,
                transformacao.DataExecucao,
                mensagem =
                    "Transformação confirmada no plano de limpeza."
            }
        );
    }

    // ========================================================
    // ELIMINAR TRANSFORMACAO
    // ========================================================

    [HttpDelete(
        "datasets/{idDataset:int}/transformacoes/{idTransformacao:long}"
    )]
    public async Task<IActionResult> EliminarTransformacao(
        int idDataset,
        long idTransformacao)
    {
        var idUtilizador =
            ObterIdUtilizador();

        var dataset =
            await ObterDatasetAsync(
                idDataset,
                idUtilizador
            );

        if (dataset is null)
        {
            return NotFound(
                "Dataset não encontrado."
            );
        }

        var transformacao =
            await _db.Transformacoes
                .FirstOrDefaultAsync(t =>
                    t.IdTransformacao ==
                        idTransformacao &&
                    t.IdDataset == idDataset &&
                    t.IdUtilizador ==
                        idUtilizador
                );

        if (transformacao is null)
        {
            return NotFound(
                "Transformação não encontrada."
            );
        }

        var descricao =
            transformacao.Descricao ??
            transformacao.Tipo;

        _db.Transformacoes.Remove(
            transformacao
        );

        await _db.SaveChangesAsync();

        await RegistarHistoricoAsync(
            dataset,
            idUtilizador,
            "Transformação eliminada",
            idTransformacao,
            descricao
        );

        return Ok(
            new
            {
                mensagem =
                    "Transformação eliminada com sucesso."
            }
        );
    }

    private static string CriarDescricao(
        string tipo,
        string? coluna,
        CriarTransformacaoRequest request)
    {
        return tipo switch
        {
            "RemoverDuplicados" =>
                "Remover registos duplicados do dataset.",

            "TratarAusentes" =>
                $"Tratar valores ausentes na coluna '{coluna}'" +
                (
                    string.IsNullOrWhiteSpace(
                        request.Estrategia
                    )
                        ? "."
                        : $" usando '{request.Estrategia}'."
                ),

            "NormalizarTexto" =>
                $"Normalizar texto da coluna '{coluna}'" +
                (
                    string.IsNullOrWhiteSpace(
                        request.Estrategia
                    )
                        ? "."
                        : $" usando '{request.Estrategia}'."
                ),

            "ConverterTipo" =>
                $"Converter a coluna '{coluna}' para " +
                $"'{request.NovoTipo ?? "tipo selecionado"}'.",

            _ =>
                "Transformação de dados."
        };
    }
}

public class CriarTransformacaoRequest
{
    public string Tipo { get; set; } = null!;

    public int? IdColuna { get; set; }

    public string? Estrategia { get; set; }

    public string? Valor { get; set; }

    public string? NovoTipo { get; set; }
}

