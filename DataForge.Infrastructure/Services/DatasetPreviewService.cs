using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataForge.Application.DTOs.Datasets;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class DatasetPreviewService
    : IDatasetPreviewService
{
    private const int TamanhoPaginaPadrao = 25;
    private const int TamanhoPaginaMaximo = 100;

    private readonly DataForgeDbContext _context;

    public DatasetPreviewService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<DatasetPreviewDto>
        ObterAsync(
            int idDataset,
            int idUtilizador,
            int pagina = 1,
            int tamanhoPagina = TamanhoPaginaPadrao)
    {
        if (pagina < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pagina),
                "A página deve ser igual ou superior a 1."
            );
        }

        if (tamanhoPagina < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanhoPagina),
                "O tamanho da página deve ser igual ou superior a 1."
            );
        }

        if (tamanhoPagina > TamanhoPaginaMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanhoPagina),
                $"O tamanho máximo permitido é {TamanhoPaginaMaximo} registos."
            );
        }

        var dataset =
            await _context.Datasets
                .AsNoTracking()
                .Include(d => d.DatasetColunas)
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
                "O dataset precisa estar processado antes de visualizar os dados."
            );
        }

        if (!string.Equals(
            dataset.TipoArquivo,
            "csv",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "O Data Preview V1 suporta apenas ficheiros CSV."
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

        var colunas =
            dataset.DatasetColunas
                .OrderBy(c => c.Posicao)
                .Select(c =>
                    new DatasetPreviewColunaDto
                    {
                        IdColuna =
                            c.IdColuna,

                        Nome =
                            c.Nome,

                        TipoDetectado =
                            c.TipoDetectado,

                        Posicao =
                            c.Posicao
                    })
                .ToList();

        var totalPaginas =
            dataset.TotalRegistos <= 0
                ? 0
                : (long)Math.Ceiling(
                    dataset.TotalRegistos /
                    (decimal)tamanhoPagina
                );

        var registos =
            new List<
                Dictionary<string, string?>
            >();

        // Quantos registos de dados devem ser ignorados.
        // O cabeçalho não faz parte desta contagem.
        var ignorar =
            ((long)pagina - 1L) *
            tamanhoPagina;

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

        if (!await csv.ReadAsync())
        {
            return ConstruirResultado(
                dataset.IdDataset,
                dataset.TotalRegistos,
                dataset.TotalColunas,
                pagina,
                tamanhoPagina,
                totalPaginas,
                colunas,
                registos
            );
        }

        csv.ReadHeader();

        var headers =
            csv.HeaderRecord ??
            Array.Empty<string>();

        long indiceRegisto = 0;

        while (await csv.ReadAsync())
        {
            if (indiceRegisto < ignorar)
            {
                indiceRegisto++;
                continue;
            }

            if (registos.Count >= tamanhoPagina)
            {
                break;
            }

            var registo =
                new Dictionary<string, string?>(
                    StringComparer.OrdinalIgnoreCase
                );

            for (
                var indiceColuna = 0;
                indiceColuna < headers.Length;
                indiceColuna++
            )
            {
                var nome =
                    headers[indiceColuna];

                // Evita colisão caso um CSV tenha cabeçalhos
                // repetidos. O profiling poderá tratar esse
                // problema de qualidade separadamente no futuro.
                if (registo.ContainsKey(nome))
                {
                    nome =
                        $"{nome}_{indiceColuna + 1}";
                }

                string? valor;

                try
                {
                    valor =
                        csv.GetField(
                            indiceColuna
                        );
                }
                catch
                {
                    valor = null;
                }

                registo[nome] =
                    string.IsNullOrWhiteSpace(valor)
                        ? null
                        : valor;
            }

            registos.Add(
                registo
            );

            indiceRegisto++;
        }

        return ConstruirResultado(
            dataset.IdDataset,
            dataset.TotalRegistos,
            dataset.TotalColunas,
            pagina,
            tamanhoPagina,
            totalPaginas,
            colunas,
            registos
        );
    }

    private static DatasetPreviewDto
        ConstruirResultado(
            int idDataset,
            long totalRegistos,
            int totalColunas,
            int pagina,
            int tamanhoPagina,
            long totalPaginas,
            IEnumerable<DatasetPreviewColunaDto> colunas,
            IEnumerable<Dictionary<string, string?>> registos)
    {
        return new DatasetPreviewDto
        {
            IdDataset =
                idDataset,

            TotalRegistos =
                totalRegistos,

            TotalColunas =
                totalColunas,

            Pagina =
                pagina,

            TamanhoPagina =
                tamanhoPagina,

            TotalPaginas =
                totalPaginas,

            TemPaginaAnterior =
                pagina > 1 &&
                totalPaginas > 0,

            TemProximaPagina =
                pagina < totalPaginas,

            Colunas =
                colunas,

            Registos =
                registos
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
