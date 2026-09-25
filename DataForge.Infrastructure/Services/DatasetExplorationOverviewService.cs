using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class DatasetExplorationOverviewService
    : IDatasetExplorationOverviewService
{
    private readonly DataForgeDbContext _context;

    public DatasetExplorationOverviewService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<ExploracaoDatasetDto> ObterAsync(
        int idDataset,
        int idUtilizador)
    {
        var dataset = await _context.Datasets
            .AsNoTracking()
            .Include(d => d.IdProjetoNavigation)
            .FirstOrDefaultAsync(d =>
                d.IdDataset == idDataset &&
                d.IdProjetoNavigation.IdUtilizador == idUtilizador &&
                d.IdProjetoNavigation.Ativo);

        if (dataset is null)
        {
            throw new KeyNotFoundException(
                "Dataset não encontrado."
            );
        }

        if (dataset.Estado != "Processado")
        {
            throw new InvalidOperationException(
                "O dataset precisa estar processado."
            );
        }


        // ====================================================
        // COLUNAS
        // ====================================================

        var colunas = await _context.DatasetColunas
            .AsNoTracking()
            .Where(c => c.IdDataset == idDataset)
            .OrderBy(c => c.Posicao)
            .ToListAsync();


        // ====================================================
        // ESTATISTICAS EXISTENTES
        // ====================================================

        var idsColunas = colunas
            .Select(c => c.IdColuna)
            .ToList();

        var idsComEstatisticas =
            await _context.EstatisticasColunas
                .AsNoTracking()
                .Where(e =>
                    idsColunas.Contains(e.IdColuna))
                .Select(e => e.IdColuna)
                .Distinct()
                .ToListAsync();

        var estatisticasSet =
            idsComEstatisticas.ToHashSet();


        // ====================================================
        // PROBLEMAS DE QUALIDADE ATIVOS
        // ====================================================

        var problemas =
            await _context.ProblemasQualidades
                .AsNoTracking()
                .Where(p =>
                    p.IdDataset == idDataset &&
                    !p.Resolvido)
                .ToListAsync();


        // ====================================================
        // ANOMALIAS ATIVAS
        // ====================================================

        var anomalias =
            await _context.Anomaliases
                .AsNoTracking()
                .Where(a =>
                    a.IdDataset == idDataset &&
                    !a.Ignorada)
                .ToListAsync();


        // ====================================================
        // RESUMO DAS COLUNAS
        // ====================================================

        var colunasDto =
            colunas.Select(coluna =>
            {
                var total =
                    coluna.TotalValores;

                var validos =
                    coluna.ValoresValidos;

                var ausentes =
                    coluna.ValoresAusentes;

                var unicos =
                    coluna.ValoresUnicos;

                var percentualValido =
                    total == 0
                        ? 0m
                        : Math.Round(
                            validos /
                            (decimal)total *
                            100m,
                            2
                        );

                var percentualAusente =
                    total == 0
                        ? 0m
                        : Math.Round(
                            ausentes /
                            (decimal)total *
                            100m,
                            2
                        );

                var percentualUnico =
                    validos == 0
                        ? 0m
                        : Math.Round(
                            unicos /
                            (decimal)validos *
                            100m,
                            2
                        );

                var totalProblemasColuna =
                    problemas.Count(p =>
                        p.IdColuna ==
                        coluna.IdColuna);

                var totalOutliersColuna =
                    anomalias.Count(a =>
                        a.IdColuna ==
                            coluna.IdColuna &&
                        a.Tipo ==
                            "Outlier Numérico");

                return new ColunaExploracaoDto
                {
                    IdColuna =
                        coluna.IdColuna,

                    Nome =
                        coluna.Nome,

                    TipoOriginal =
                        coluna.TipoOriginal,

                    TipoDetectado =
                        coluna.TipoDetectado,

                    PapelSemantico =
                        SemanticRoleClassifier.Detectar(
                            coluna.Nome,
                            coluna.TipoDetectado,
                            validos,
                            unicos
                        ),

                    Posicao =
                        coluna.Posicao,

                    TotalValores =
                        total,

                    ValoresValidos =
                        validos,

                    ValoresAusentes =
                        ausentes,

                    ValoresUnicos =
                        unicos,

                    PercentualValido =
                        percentualValido,

                    PercentualAusente =
                        percentualAusente,

                    PercentualUnico =
                        percentualUnico,

                    TotalProblemas =
                        totalProblemasColuna,

                    TotalOutliers =
                        totalOutliersColuna,

                    TemEstatisticasNumericas =
                        estatisticasSet.Contains(
                            coluna.IdColuna
                        )
                };
            })
            .ToList();


        // ====================================================
        // RESUMO GERAL DE QUALIDADE
        // ====================================================

        var totalDuplicados =
            anomalias.Count(a =>
                a.Tipo == "Linha Duplicada");

        var totalOutliers =
            anomalias.Count(a =>
                a.Tipo == "Outlier Numérico");

        var totalValoresAusentes =
            colunas.Sum(c =>
                c.ValoresAusentes);


        var qualidade =
            new ResumoQualidadeExploracaoDto
            {
                TotalProblemas =
                    problemas.Count,

                ProblemasCriticos =
                    problemas.Count(p =>
                        p.Severidade ==
                        "Critico"),

                ProblemasErro =
                    problemas.Count(p =>
                        p.Severidade ==
                        "Erro"),

                ProblemasAviso =
                    problemas.Count(p =>
                        p.Severidade ==
                        "Aviso"),

                ProblemasInformacao =
                    problemas.Count(p =>
                        p.Severidade ==
                        "Informacao"),

                TotalDuplicados =
                    totalDuplicados,

                TotalOutliers =
                    totalOutliers,

                TotalValoresAusentes =
                    checked((int)totalValoresAusentes)
            };


        return new ExploracaoDatasetDto
        {
            IdDataset =
                dataset.IdDataset,

            Nome =
                dataset.Nome,

            NomeOriginal =
                dataset.NomeOriginal,

            TipoArquivo =
                dataset.TipoArquivo,

            Estado =
                dataset.Estado,

            TotalRegistos =
                dataset.TotalRegistos,

            TotalColunas =
                dataset.TotalColunas,

            TamanhoBytes =
                dataset.TamanhoBytes,

            QualityScore =
                dataset.QualityScore,

            DataImportacao =
                dataset.DataImportacao,

            DataProcessamento =
                dataset.DataProcessamento,

            Qualidade =
                qualidade,

            Colunas =
                colunasDto
        };
    }


    // ========================================================
    // PAPEL SEMANTICO
    //
    // Mesma regra validada na Exploration V2.
    // ========================================================

}


