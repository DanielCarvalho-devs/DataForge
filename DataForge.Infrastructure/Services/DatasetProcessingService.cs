using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class DatasetProcessingService : IDatasetProcessingService
{
    private readonly DataForgeDbContext _context;

    public DatasetProcessingService(
        DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task ProcessarAsync(
        int idDataset,
        int idUtilizador)
    {
        var dataset = await _context.Datasets
            .Include(d => d.IdProjetoNavigation)
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
            dataset.TipoArquivo,
            "csv",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "Nesta etapa, o processamento automático está disponível apenas para CSV."
            );
        }

        if (string.IsNullOrWhiteSpace(
            dataset.CaminhoArquivo))
        {
            throw new InvalidOperationException(
                "O dataset não possui caminho de ficheiro."
            );
        }

        var caminhoArquivo =
            Path.IsPathRooted(dataset.CaminhoArquivo)
                ? dataset.CaminhoArquivo
                : Path.Combine(
                    AppContext.BaseDirectory,
                    dataset.CaminhoArquivo
                );

        // Quando a API executa, AppContext.BaseDirectory aponta para bin/.
        // O ficheiro foi guardado relativamente ao ContentRoot da API.
        // Procuramos também a partir do diretório atual.
        if (!File.Exists(caminhoArquivo))
        {
            caminhoArquivo =
                Path.GetFullPath(
                    dataset.CaminhoArquivo,
                    Directory.GetCurrentDirectory()
                );
        }

        if (!File.Exists(caminhoArquivo))
        {
            throw new FileNotFoundException(
                "O ficheiro físico do dataset não foi encontrado.",
                caminhoArquivo
            );
        }

        dataset.Estado = "A Processar";
        await _context.SaveChangesAsync();

        try
        {
            var colunasAntigas =
                await _context.DatasetColunas
                    .Where(c =>
                        c.IdDataset == idDataset)
                    .ToListAsync();

            if (colunasAntigas.Count > 0)
            {
                _context.DatasetColunas
                    .RemoveRange(colunasAntigas);

                await _context.SaveChangesAsync();
            }

            using var reader =
                new StreamReader(caminhoArquivo);

            var configuracao =
                new CsvConfiguration(
                    CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    TrimOptions = TrimOptions.Trim,
                    DetectDelimiter = true
                };

            using var csv =
                new CsvReader(
                    reader,
                    configuracao);

            if (!await csv.ReadAsync())
            {
                throw new InvalidOperationException(
                    "O ficheiro CSV está vazio."
                );
            }

            csv.ReadHeader();

            var headers =
                csv.HeaderRecord
                ?? throw new InvalidOperationException(
                    "Não foi possível identificar o cabeçalho do CSV."
                );

            if (headers.Length == 0)
            {
                throw new InvalidOperationException(
                    "O CSV não possui colunas."
                );
            }

            var analises =
                headers
                    .Select(
                        (nome, indice) =>
                            new AnaliseColuna(
                                nome,
                                indice
                            ))
                    .ToList();

            long totalRegistos = 0;

            while (await csv.ReadAsync())
            {
                totalRegistos++;

                for (
                    var i = 0;
                    i < analises.Count;
                    i++)
                {
                    string? valor;

                    try
                    {
                        valor = csv.GetField(i);
                    }
                    catch
                    {
                        valor = null;
                    }

                    analises[i]
                        .AdicionarValor(valor);
                }
            }

            foreach (var analise in analises)
            {
                var coluna =
                    new DatasetColuna
                    {
                        IdDataset =
                            dataset.IdDataset,

                        Nome =
                            string.IsNullOrWhiteSpace(
                                analise.Nome)
                                ? $"Coluna_{analise.Posicao + 1}"
                                : analise.Nome.Trim(),

                        TipoOriginal = "string",

                        TipoDetectado =
                            analise.DetectarTipo(),

                        Posicao =
                            analise.Posicao,

                        TotalValores =
                            totalRegistos,

                        ValoresValidos =
                            analise.ValoresValidos,

                        ValoresAusentes =
                            analise.ValoresAusentes,

                        ValoresUnicos =
                            analise.ValoresUnicos,

                        PercentualValido =
                            totalRegistos == 0
                                ? 0
                                : Math.Round(
                                    (
                                        (decimal)analise.ValoresValidos
                                        / totalRegistos
                                    ) * 100,
                                    2
                                )
                    };

                _context.DatasetColunas
                    .Add(coluna);
            }

            dataset.TotalRegistos =
                totalRegistos;

            dataset.TotalColunas =
                headers.Length;

            dataset.Estado =
                "Processado";

            dataset.DataProcessamento =
                DateTime.Now;

            await _context.SaveChangesAsync();
        }
        catch
        {
            dataset.Estado = "Erro";

            dataset.DataProcessamento =
                DateTime.Now;

            await _context.SaveChangesAsync();

            throw;
        }
    }

    private sealed class AnaliseColuna
    {
        private readonly HashSet<string>
            _valoresUnicos =
                new(
                    StringComparer.Ordinal
                );

        private bool _podeSerInteiro = true;
        private bool _podeSerDecimal = true;
        private bool _podeSerData = true;
        private bool _podeSerBooleano = true;

        public AnaliseColuna(
            string nome,
            int posicao)
        {
            Nome = nome;
            Posicao = posicao;
        }

        public string Nome { get; }

        public int Posicao { get; }

        public long ValoresValidos { get; private set; }

        public long ValoresAusentes { get; private set; }

        public long ValoresUnicos =>
            _valoresUnicos.Count;

        public void AdicionarValor(
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                ValoresAusentes++;
                return;
            }

            valor = valor.Trim();

            ValoresValidos++;

            _valoresUnicos.Add(valor);

            if (!long.TryParse(
                valor,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _))
            {
                _podeSerInteiro = false;
            }

            if (!decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out _))
            {
                _podeSerDecimal = false;
            }

            if (!DateTime.TryParse(
                valor,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            {
                _podeSerData = false;
            }

            if (!bool.TryParse(
                valor,
                out _))
            {
                _podeSerBooleano = false;
            }
        }

        public string DetectarTipo()
        {
            if (ValoresValidos == 0)
            {
                return "Desconhecido";
            }

            if (_podeSerInteiro)
            {
                return "Inteiro";
            }

            if (_podeSerDecimal)
            {
                return "Decimal";
            }

            if (_podeSerData)
            {
                return "Data";
            }

            if (_podeSerBooleano)
            {
                return "Booleano";
            }

            return "Texto";
        }
    }
}


