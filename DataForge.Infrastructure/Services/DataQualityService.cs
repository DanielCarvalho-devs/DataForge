using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class DataQualityService : IDataQualityService
{
    private readonly DataForgeDbContext _context;
    private readonly IQualityScoreService _qualityScoreService;

    public DataQualityService(
        DataForgeDbContext context,
        IQualityScoreService qualityScoreService)
    {
        _context = context;
        _qualityScoreService = qualityScoreService;
    }

    public async Task<QualidadeDatasetDto> AnalisarAsync(
        int idDataset,
        int idUtilizador)
    {
        var dataset = await _context.Datasets
            .Include(d => d.IdProjetoNavigation)
            .Include(d => d.DatasetColunas)
            .FirstOrDefaultAsync(d =>
                d.IdDataset == idDataset &&
                d.IdProjetoNavigation.IdUtilizador == idUtilizador &&
                d.IdProjetoNavigation.Ativo);

        if (dataset is null)
        {
            throw new ArgumentException(
                "Dataset não encontrado."
            );
        }

        if (!string.Equals(
            dataset.Estado,
            "Processado",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "O dataset precisa estar processado antes da análise de qualidade."
            );
        }

        if (dataset.DatasetColunas.Count == 0)
        {
            throw new InvalidOperationException(
                "O dataset não possui profiling de colunas."
            );
        }

        // ----------------------------------------------------
        // Idempotência:
        // remove problemas NÃO resolvidos gerados anteriormente.
        // Problemas resolvidos permanecem como histórico.
        // ----------------------------------------------------

        var problemasAnteriores =
            await _context.ProblemasQualidades
                .Where(p =>
                    p.IdDataset == idDataset &&
                    p.Tipo == "Valor Ausente" &&
                    !p.Resolvido)
                .ToListAsync();

        if (problemasAnteriores.Count > 0)
        {
            _context.ProblemasQualidades
                .RemoveRange(problemasAnteriores);
        }

        // ----------------------------------------------------
        // COMPLETUDE GLOBAL
        // ----------------------------------------------------

        long totalCelulas =
            dataset.DatasetColunas.Sum(
                c => c.TotalValores
            );

        long valoresValidos =
            dataset.DatasetColunas.Sum(
                c => c.ValoresValidos
            );

        long valoresAusentes =
            dataset.DatasetColunas.Sum(
                c => c.ValoresAusentes
            );

        decimal completude =
            totalCelulas == 0
                ? 100m
                : Math.Round(
                    ((decimal)valoresValidos / totalCelulas)
                    * 100m,
                    2
                );
        // ----------------------------------------------------
        // PROBLEMAS DE VALORES AUSENTES
        // ----------------------------------------------------

        foreach (
            var coluna in dataset.DatasetColunas
                .Where(c => c.ValoresAusentes > 0)
        )
        {
            var percentualAusente =
                coluna.TotalValores == 0
                    ? 0m
                    : Math.Round(
                        ((decimal)coluna.ValoresAusentes
                        / coluna.TotalValores)
                        * 100m,
                        2
                    );

            var severidade =
                ObterSeveridade(percentualAusente);

            var problema =
                new ProblemasQualidade
                {
                    IdDataset =
                        dataset.IdDataset,

                    IdColuna =
                        coluna.IdColuna,

                    Tipo =
                        "Valor Ausente",

                    Severidade =
                        severidade,

                    Descricao =
                        $"A coluna '{coluna.Nome}' possui " +
                        $"{coluna.ValoresAusentes} valor(es) ausente(s) " +
                        $"de {coluna.TotalValores} " +
                        $"({percentualAusente:0.00}%).",

                    Quantidade =
                        coluna.ValoresAusentes,

                    Resolvido =
                        false,

                    DataDeteccao =
                        DateTime.Now,

                    DataResolucao =
                        null
                };

            _context.ProblemasQualidades
                .Add(problema);
        }

        await _context.SaveChangesAsync();
await _qualityScoreService.RecalcularAsync(
            dataset.IdDataset
        );

        return await ConstruirResultadoAsync(
            dataset.IdDataset
        );
    }

    public async Task<QualidadeDatasetDto?> ObterAsync(
        int idDataset,
        int idUtilizador)
    {
        var autorizado =
            await _context.Datasets
                .AsNoTracking()
                .AnyAsync(d =>
                    d.IdDataset == idDataset &&
                    d.IdProjetoNavigation.IdUtilizador == idUtilizador &&
                    d.IdProjetoNavigation.Ativo);

        if (!autorizado)
        {
            return null;
        }

        return await ConstruirResultadoAsync(
            idDataset
        );
    }

    private async Task<QualidadeDatasetDto>
        ConstruirResultadoAsync(
            int idDataset)
    {
        var dataset =
            await _context.Datasets
                .AsNoTracking()
                .FirstAsync(d =>
                    d.IdDataset == idDataset);

        var colunas =
            await _context.DatasetColunas
                .AsNoTracking()
                .Where(c =>
                    c.IdDataset == idDataset)
                .ToListAsync();

        var problemas =
            await _context.ProblemasQualidades
                .AsNoTracking()
                .Where(p =>
                    p.IdDataset == idDataset &&
                    !p.Resolvido)
                .OrderBy(p =>
                    p.IdColuna)
                .Select(p =>
                    new ProblemaQualidadeDto
                    {
                        IdProblema =
                            p.IdProblema,

                        IdDataset =
                            p.IdDataset,

                        IdColuna =
                            p.IdColuna,

                        NomeColuna =
                            p.IdColunaNavigation != null
                                ? p.IdColunaNavigation.Nome
                                : null,

                        Tipo =
                            p.Tipo,

                        Severidade =
                            p.Severidade,

                        Descricao =
                            p.Descricao,

                        Quantidade =
                            p.Quantidade,

                        Resolvido =
                            p.Resolvido,

                        DataDeteccao =
                            p.DataDeteccao,

                        DataResolucao =
                            p.DataResolucao
                    })
                .ToListAsync();

        long totalCelulas =
            colunas.Sum(c =>
                c.TotalValores);

        long valoresValidos =
            colunas.Sum(c =>
                c.ValoresValidos);

        long valoresAusentes =
            colunas.Sum(c =>
                c.ValoresAusentes);

        decimal completude =
            totalCelulas == 0
                ? 100m
                : Math.Round(
                    ((decimal)valoresValidos
                    / totalCelulas)
                    * 100m,
                    2
                );

        var linhasDuplicadas =
            await _context.Anomaliases
                .AsNoTracking()
                .LongCountAsync(a =>
                    a.IdDataset == idDataset &&
                    a.Tipo == "Linha Duplicada" &&
                    !a.Ignorada);

        decimal unicidade =
            dataset.TotalRegistos <= 0
                ? 100m
                : Math.Round(
                    ((decimal)Math.Max(
                        0L,
                        dataset.TotalRegistos -
                        linhasDuplicadas
                    ) / dataset.TotalRegistos)
                    * 100m,
                    2
                );

        return new QualidadeDatasetDto
        {
            IdDataset =
                dataset.IdDataset,

            QualityScore =
                dataset.QualityScore
                ?? completude,

            TotalCelulas =
                totalCelulas,

            ValoresValidos =
                valoresValidos,

            ValoresAusentes =
                valoresAusentes,

            PercentualCompletude =
                completude,

            PercentualUnicidade =
                unicidade,

            TotalProblemas =
                problemas.Count,

            ProblemasCriticos =
                problemas.Count(p =>
                    p.Severidade == "Critico"),

            ProblemasErro =
                problemas.Count(p =>
                    p.Severidade == "Erro"),

            ProblemasAviso =
                problemas.Count(p =>
                    p.Severidade == "Aviso"),

            ProblemasInformacao =
                problemas.Count(p =>
                    p.Severidade == "Informacao"),

            Problemas =
                problemas
        };
    }

    private static string ObterSeveridade(
        decimal percentualAusente)
    {
        if (percentualAusente > 50m)
        {
            return "Critico";
        }

        if (percentualAusente > 20m)
        {
            return "Erro";
        }

        if (percentualAusente > 5m)
        {
            return "Aviso";
        }

        return "Informacao";
    }
}



