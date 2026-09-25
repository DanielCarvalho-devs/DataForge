using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class DatasetStatisticsService
    : IDatasetStatisticsService
{
    private readonly DataForgeDbContext _context;

    public DatasetStatisticsService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EstatisticaColunaDto>>
        CalcularAsync(
            int idDataset,
            int idUtilizador)
    {
        var dataset = await _context.Datasets
            .Include(d => d.IdProjetoNavigation)
            .Include(d => d.DatasetColunas)
                .ThenInclude(c => c.EstatisticasColunas)
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
                "O dataset precisa estar processado antes do cálculo estatístico."
            );
        }

        if (!string.Equals(
            dataset.TipoArquivo,
            "csv",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "O Statistical Engine V1 suporta apenas ficheiros CSV."
            );
        }

        if (string.IsNullOrWhiteSpace(
            dataset.CaminhoArquivo))
        {
            throw new FileNotFoundException(
                "O caminho físico do dataset não está definido."
            );
        }

        var colunasNumericas =
            dataset.DatasetColunas
                .Where(c =>
                    c.TipoDetectado == "Inteiro" ||
                    c.TipoDetectado == "Decimal")
                .OrderBy(c => c.Posicao)
                .ToList();

        if (colunasNumericas.Count == 0)
        {
            return Enumerable.Empty<EstatisticaColunaDto>();
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

        var valoresPorColuna =
            colunasNumericas.ToDictionary(
                c => c.IdColuna,
                _ => new List<decimal>()
            );

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
            new StreamReader(caminhoArquivo);

        using var csv =
            new CsvReader(reader, config);

        if (!await csv.ReadAsync())
        {
            throw new InvalidOperationException(
                "O ficheiro CSV está vazio."
            );
        }

        csv.ReadHeader();

        var header =
            csv.HeaderRecord;

        if (header is null ||
            header.Length == 0)
        {
            throw new InvalidOperationException(
                "O ficheiro CSV não possui cabeçalho."
            );
        }

        while (await csv.ReadAsync())
        {
            foreach (var coluna in colunasNumericas)
            {
                if (coluna.Posicao < 0 ||
                    coluna.Posicao >= header.Length)
                {
                    continue;
                }

                string? valorTexto = null;

                try
                {
                    valorTexto =
                        csv.GetField(
                            coluna.Posicao
                        );
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                    valorTexto))
                {
                    continue;
                }

                if (decimal.TryParse(
                    valorTexto,
                    NumberStyles.Number |
                    NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out var valor))
                {
                    valoresPorColuna[
                        coluna.IdColuna
                    ].Add(valor);
                }
            }
        }

        // ----------------------------------------------------
        // IDEMPOTENCIA
        // ----------------------------------------------------

        var idsColunas =
            colunasNumericas
                .Select(c => c.IdColuna)
                .ToList();

        var antigas =
            await _context.EstatisticasColunas
                .Where(e =>
                    idsColunas.Contains(
                        e.IdColuna
                    ))
                .ToListAsync();

        if (antigas.Count > 0)
        {
            _context.EstatisticasColunas
                .RemoveRange(antigas);
        }

        // ----------------------------------------------------
        // CALCULO
        // ----------------------------------------------------

        foreach (var coluna in colunasNumericas)
        {
            var valores =
                valoresPorColuna[
                    coluna.IdColuna
                ];

            if (valores.Count == 0)
            {
                continue;
            }

            valores.Sort();

            var media =
                valores.Average();

            var mediana =
                Percentil(valores, 0.50m);

            var estatistica =
                new EstatisticasColuna
                {
                    IdColuna =
                        coluna.IdColuna,

                    Minimo =
                        valores.First(),

                    Maximo =
                        valores.Last(),

                    Media =
                        Arredondar(media),

                    Mediana =
                        Arredondar(mediana),

                    DesvioPadrao =
                        Arredondar(
                            CalcularDesvioPadrao(
                                valores,
                                media
                            )
                        ),

                    P25 =
                        Arredondar(
                            Percentil(
                                valores,
                                0.25m
                            )
                        ),

                    P50 =
                        Arredondar(
                            Percentil(
                                valores,
                                0.50m
                            )
                        ),

                    P75 =
                        Arredondar(
                            Percentil(
                                valores,
                                0.75m
                            )
                        ),

                    P90 =
                        Arredondar(
                            Percentil(
                                valores,
                                0.90m
                            )
                        ),

                    P99 =
                        Arredondar(
                            Percentil(
                                valores,
                                0.99m
                            )
                        ),

                    DataCalculo =
                        DateTime.Now
                };

            _context.EstatisticasColunas
                .Add(estatistica);
        }

        await _context.SaveChangesAsync();

        return await ConstruirResultadoAsync(
            idDataset
        );
    }

    public async Task<IEnumerable<EstatisticaColunaDto>?>
        ObterAsync(
            int idDataset,
            int idUtilizador)
    {
        var autorizado =
            await _context.Datasets
                .AsNoTracking()
                .AnyAsync(d =>
                    d.IdDataset == idDataset &&
                    d.IdProjetoNavigation.IdUtilizador ==
                        idUtilizador &&
                    d.IdProjetoNavigation.Ativo);

        if (!autorizado)
        {
            return null;
        }

        return await ConstruirResultadoAsync(
            idDataset
        );
    }

    private async Task<List<EstatisticaColunaDto>>
        ConstruirResultadoAsync(
            int idDataset)
    {
        return await _context.EstatisticasColunas
            .AsNoTracking()
            .Where(e =>
                e.IdColunaNavigation.IdDataset ==
                    idDataset)
            .OrderBy(e =>
                e.IdColunaNavigation.Posicao)
            .Select(e =>
                new EstatisticaColunaDto
                {
                    IdEstatistica =
                        e.IdEstatistica,

                    IdColuna =
                        e.IdColuna,

                    NomeColuna =
                        e.IdColunaNavigation.Nome,

                    TipoDetectado =
                        e.IdColunaNavigation
                            .TipoDetectado,

                    Minimo =
                        e.Minimo,

                    Maximo =
                        e.Maximo,

                    Media =
                        e.Media,

                    Mediana =
                        e.Mediana,

                    DesvioPadrao =
                        e.DesvioPadrao,

                    P25 =
                        e.P25,

                    P50 =
                        e.P50,

                    P75 =
                        e.P75,

                    P90 =
                        e.P90,

                    P99 =
                        e.P99,

                    DataCalculo =
                        e.DataCalculo
                })
            .ToListAsync();
    }

    private static decimal CalcularDesvioPadrao(
        IReadOnlyCollection<decimal> valores,
        decimal media)
    {
        if (valores.Count <= 1)
        {
            return 0m;
        }

        decimal somaQuadrados = 0m;

        foreach (var valor in valores)
        {
            var diferenca =
                valor - media;

            somaQuadrados +=
                diferenca * diferenca;
        }

        // Desvio-padrão populacional.
        var variancia =
            somaQuadrados /
            valores.Count;

        return (decimal)Math.Sqrt(
            (double)variancia
        );
    }

    private static decimal Percentil(
        IReadOnlyList<decimal> valores,
        decimal percentil)
    {
        if (valores.Count == 0)
        {
            return 0m;
        }

        if (valores.Count == 1)
        {
            return valores[0];
        }

        // Percentil com interpolação linear:
        // índice = (n - 1) * p
        var indice =
            (valores.Count - 1) *
            percentil;

        var inferior =
            (int)Math.Floor(
                (double)indice
            );

        var superior =
            (int)Math.Ceiling(
                (double)indice
            );

        if (inferior == superior)
        {
            return valores[inferior];
        }

        var fracao =
            indice - inferior;

        return valores[inferior] +
            (
                valores[superior] -
                valores[inferior]
            ) * fracao;
    }

    private static decimal Arredondar(
        decimal valor)
    {
        return Math.Round(
            valor,
            10,
            MidpointRounding.AwayFromZero
        );
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

        return Path.GetFullPath(
            Path.Combine(
                Directory.GetCurrentDirectory(),
                caminho
            )
        );
    }
}
