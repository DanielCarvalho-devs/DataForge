using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class ExplorationService
    : IExplorationService
{
    private const int TopPadrao = 10;
    private const int TopMaximo = 100;

    private readonly DataForgeDbContext _context;

    public ExplorationService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<DistribuicaoColunaDto>
        ObterDistribuicaoAsync(
            int idDataset,
            int idColuna,
            int idUtilizador,
            int top = TopPadrao)
    {
        if (top < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(top),
                "O parâmetro top deve ser igual ou superior a 1."
            );
        }

        if (top > TopMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(top),
                $"O parâmetro top não pode ser superior a {TopMaximo}."
            );
        }

        var dataset =
            await _context.Datasets
                .AsNoTracking()
                .Include(d => d.IdProjetoNavigation)
                .FirstOrDefaultAsync(d =>
                    d.IdDataset == idDataset &&
                    d.IdProjetoNavigation.IdUtilizador ==
                        idUtilizador &&
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
                "O dataset precisa estar processado antes da exploração."
            );
        }

        if (!string.Equals(
            dataset.TipoArquivo,
            "csv",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "A Exploration API V1 suporta apenas ficheiros CSV."
            );
        }

        var coluna =
            await _context.DatasetColunas
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IdColuna == idColuna &&
                    c.IdDataset == idDataset);

        if (coluna is null)
        {
            throw new KeyNotFoundException(
                "Coluna não encontrada neste dataset."
            );
        }

        if (string.IsNullOrWhiteSpace(
            dataset.CaminhoArquivo))
        {
            throw new FileNotFoundException(
                "O caminho físico do dataset não está definido."
            );
        }

        var caminhoArquivo =
            ResolverCaminhoArquivo(
                dataset.CaminhoArquivo
            );

        if (!File.Exists(caminhoArquivo))
        {
            throw new FileNotFoundException(
                "O ficheiro físico do dataset não foi encontrado.",
                caminhoArquivo
            );
        }

        var frequencias =
            new Dictionary<string, long>(
                StringComparer.Ordinal
            );

        long totalValores = 0;
        long valoresAusentes = 0;

        var config =
            new CsvConfiguration(
                CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
                DetectDelimiter = true
            };

        using var reader =
            new StreamReader(
                caminhoArquivo
            );

        using var csv =
            new CsvReader(
                reader,
                config
            );

        if (await csv.ReadAsync())
        {
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                totalValores++;

                string? valor;

                try
                {
                    valor =
                        csv.GetField(
                            coluna.Posicao
                        );
                }
                catch
                {
                    valor = null;
                }

                if (string.IsNullOrWhiteSpace(valor))
                {
                    valoresAusentes++;
                    continue;
                }

                valor = valor.Trim();

                if (frequencias.TryGetValue(
                    valor,
                    out var quantidadeAtual))
                {
                    frequencias[valor] =
                        quantidadeAtual + 1;
                }
                else
                {
                    frequencias[valor] = 1;
                }
            }
        }

        var valoresValidos =
            totalValores -
            valoresAusentes;

        var valoresDistintos =
            frequencias.LongCount();

        var distribuicao =
            frequencias
                .OrderByDescending(x => x.Value)
                .ThenBy(
                    x => x.Key,
                    StringComparer.Ordinal
                )
                .Take(top)
                .Select(x =>
                    new ItemDistribuicaoDto
                    {
                        Valor =
                            x.Key,

                        Quantidade =
                            x.Value,

                        Percentual =
                            totalValores == 0
                                ? 0m
                                : Math.Round(
                                    x.Value /
                                    (decimal)totalValores *
                                    100m,
                                    2
                                )
                    })
                .ToList();

        return new DistribuicaoColunaDto
        {
            IdDataset =
                dataset.IdDataset,

            IdColuna =
                coluna.IdColuna,

            NomeColuna =
                coluna.Nome,

            TipoDetectado =
                coluna.TipoDetectado,

            TotalValores =
                totalValores,

            ValoresValidos =
                valoresValidos,

            ValoresAusentes =
                valoresAusentes,

            ValoresDistintos =
                valoresDistintos,

            TopSolicitado =
                top,

            DistribuicaoTruncada =
                valoresDistintos > top,

            Distribuicao =
                distribuicao
        };
    }

    private static string ResolverCaminhoArquivo(
        string caminho)
    {
        if (Path.IsPathRooted(caminho))
        {
            return caminho;
        }

        var caminhoBase =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    caminho
                )
            );

        if (File.Exists(caminhoBase))
        {
            return caminhoBase;
        }

        var diretorioAtual =
            Directory.GetCurrentDirectory();

        return Path.GetFullPath(
            Path.Combine(
                diretorioAtual,
                caminho
            )
        );
    }
}
