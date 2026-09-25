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
[Route("api/dataset-intelligence")]
public class DatasetIntelligenceController : ControllerBase
{
    private readonly DataForgeDbContext _db;

    public DatasetIntelligenceController(
        DataForgeDbContext db)
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
        string entidade,
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
                Entidade = entidade,
                IdRegisto = idRegisto,
                Descricao = descricao,
                DataAcao = DateTime.Now
            }
        );

        await _db.SaveChangesAsync();
    }

    // ========================================================
    // ANALISES
    // ========================================================

    [HttpGet("datasets/{idDataset:int}/analises")]
    public async Task<IActionResult> ObterAnalises(
        int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

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

        var analises =
            await _db.Analises
                .AsNoTracking()
                .Where(a =>
                    a.IdDataset == idDataset &&
                    a.IdUtilizador == idUtilizador
                )
                .OrderByDescending(a =>
                    a.DataCriacao
                )
                .Select(a => new
                {
                    a.IdAnalise,
                    a.IdDataset,
                    a.Nome,
                    a.Tipo,
                    a.ConfiguracaoJson,
                    a.ResultadoJson,
                    a.DataCriacao,
                    a.DataAtualizacao
                })
                .ToListAsync();

        return Ok(analises);
    }

    [HttpPost("datasets/{idDataset:int}/analises")]
    public async Task<IActionResult> CriarAnalise(
        int idDataset,
        [FromBody] CriarAnaliseRequest request)
    {
        var idUtilizador = ObterIdUtilizador();

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

        var nome =
            string.IsNullOrWhiteSpace(request.Nome)
                ? $"Análise {DateTime.Now:dd/MM/yyyy HH:mm}"
                : request.Nome.Trim();

        if (nome.Length > 200)
        {
            return BadRequest(
                "O nome da análise não pode ultrapassar 200 caracteres."
            );
        }

        var problemas =
            await _db.ProblemasQualidades
                .AsNoTracking()
                .Where(p =>
                    p.IdDataset == idDataset &&
                    !p.Resolvido
                )
                .ToListAsync();

        var anomalias =
            await _db.Anomaliases
                .AsNoTracking()
                .Where(a =>
                    a.IdDataset == idDataset &&
                    !a.Ignorada
                )
                .ToListAsync();

        var totalProblemas =
            problemas.Count;

        var problemasPorTipo =
            problemas
                .GroupBy(p => p.Tipo)
                .Select(g => new
                {
                    Tipo = g.Key,
                    Quantidade = g.Count()
                })
                .OrderByDescending(x =>
                    x.Quantidade
                )
                .ToList();

        var severidades =
            problemas
                .GroupBy(p => p.Severidade)
                .Select(g => new
                {
                    Severidade = g.Key,
                    Quantidade = g.Count()
                })
                .OrderByDescending(x =>
                    x.Quantidade
                )
                .ToList();

        var resultado = new
        {
            Dataset = dataset.Nome,
            dataset.TotalRegistos,
            dataset.TotalColunas,
            dataset.QualityScore,
            TotalProblemas = totalProblemas,
            TotalAnomalias = anomalias.Count,
            ProblemasPorTipo = problemasPorTipo,
            Severidades = severidades,
            GeradoEm = DateTime.Now
        };

        var analise =
            new Analise
            {
                IdDataset = idDataset,
                IdUtilizador = idUtilizador,
                Nome = nome,
                Tipo = "ResumoDataset",
                ConfiguracaoJson =
                    JsonSerializer.Serialize(
                        new
                        {
                            Origem = "DataForge",
                            Versao = 1
                        }
                    ),
                ResultadoJson =
                    JsonSerializer.Serialize(
                        resultado
                    ),
                DataCriacao = DateTime.Now
            };

        _db.Analises.Add(analise);

        await _db.SaveChangesAsync();

        await RegistarHistoricoAsync(
            dataset,
            idUtilizador,
            "Análise criada",
            "Analise",
            analise.IdAnalise,
            $"A análise '{analise.Nome}' foi criada."
        );

        return Ok(
            new
            {
                analise.IdAnalise,
                analise.IdDataset,
                analise.Nome,
                analise.Tipo,
                analise.ConfiguracaoJson,
                analise.ResultadoJson,
                analise.DataCriacao,
                analise.DataAtualizacao
            }
        );
    }

    [HttpDelete(
        "datasets/{idDataset:int}/analises/{idAnalise:long}"
    )]
    public async Task<IActionResult> EliminarAnalise(
        int idDataset,
        long idAnalise)
    {
        var idUtilizador = ObterIdUtilizador();

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

        var analise =
            await _db.Analises
                .FirstOrDefaultAsync(a =>
                    a.IdAnalise == idAnalise &&
                    a.IdDataset == idDataset &&
                    a.IdUtilizador == idUtilizador
                );

        if (analise is null)
        {
            return NotFound(
                "Análise não encontrada."
            );
        }

        var nome = analise.Nome;

        _db.Analises.Remove(analise);

        await _db.SaveChangesAsync();

        await RegistarHistoricoAsync(
            dataset,
            idUtilizador,
            "Análise eliminada",
            "Analise",
            idAnalise,
            $"A análise '{nome}' foi eliminada."
        );

        return Ok(
            new
            {
                mensagem =
                    "Análise eliminada com sucesso."
            }
        );
    }

    // ========================================================
    // RELATORIO
    // ========================================================

    [HttpGet(
        "datasets/{idDataset:int}/relatorio"
    )]
    public async Task<IActionResult> ObterRelatorio(
        int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

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

        var problemas =
            await _db.ProblemasQualidades
                .AsNoTracking()
                .Where(p =>
                    p.IdDataset == idDataset &&
                    !p.Resolvido
                )
                .ToListAsync();

        var anomalias =
            await _db.Anomaliases
                .AsNoTracking()
                .Where(a =>
                    a.IdDataset == idDataset &&
                    !a.Ignorada
                )
                .ToListAsync();

        var totalAnalises =
            await _db.Analises
                .AsNoTracking()
                .CountAsync(a =>
                    a.IdDataset == idDataset &&
                    a.IdUtilizador == idUtilizador
                );

        var tiposProblema =
            problemas
                .GroupBy(p => p.Tipo)
                .Select(g => new
                {
                    Tipo = g.Key,
                    Quantidade = g.Count()
                })
                .OrderByDescending(x =>
                    x.Quantidade
                )
                .ToList();

        return Ok(
            new
            {
                Dataset = new
                {
                    dataset.IdDataset,
                    dataset.Nome,
                    dataset.NomeOriginal,
                    dataset.TipoArquivo,
                    dataset.TamanhoBytes,
                    dataset.TotalRegistos,
                    dataset.TotalColunas,
                    dataset.QualityScore,
                    dataset.Estado,
                    dataset.DataImportacao,
                    dataset.DataProcessamento
                },

                Resumo = new
                {
                    TotalProblemas =
                        problemas.Count,

                    TotalAnomalias =
                        anomalias.Count,

                    TotalAnalises =
                        totalAnalises,

                    ColunasValidas =
                        colunas.Count(c =>
                            (c.PercentualValido ?? 0) >= 95
                        )
                },

                ProblemasPorTipo =
                    tiposProblema,

                Colunas = colunas,

                GeradoEm = DateTime.Now
            }
        );
    }

    [HttpPost(
        "datasets/{idDataset:int}/relatorio/gerar"
    )]
    public async Task<IActionResult> GerarRelatorio(
        int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

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

        await RegistarHistoricoAsync(
            dataset,
            idUtilizador,
            "Relatório gerado",
            "Relatorio",
            dataset.IdDataset,
            $"Relatório do dataset '{dataset.Nome}' gerado."
        );

        return Ok(
            new
            {
                mensagem =
                    "Relatório registado com sucesso.",
                geradoEm = DateTime.Now
            }
        );
    }

    // ========================================================
    // HISTORICO
    // ========================================================

    [HttpGet(
        "datasets/{idDataset:int}/historico"
    )]
    public async Task<IActionResult> ObterHistorico(
        int idDataset)
    {
        var idUtilizador = ObterIdUtilizador();

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

        var historico =
            await _db.Historicos
                .AsNoTracking()
                .Where(h =>
                    h.IdDataset == idDataset
                )
                .OrderByDescending(h =>
                    h.DataAcao
                )
                .Select(h => new
                {
                    h.IdHistorico,
                    h.IdUtilizador,
                    h.IdProjeto,
                    h.IdDataset,
                    h.Acao,
                    h.Entidade,
                    h.IdRegisto,
                    h.Descricao,
                    h.DataAcao
                })
                .ToListAsync();

        return Ok(historico);
    }
}

public class CriarAnaliseRequest
{
    public string? Nome { get; set; }
}
