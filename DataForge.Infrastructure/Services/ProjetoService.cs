using DataForge.Application.DTOs.Projetos;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Services;

public class ProjetoService : IProjetoService
{
    private readonly DataForgeDbContext _context;

    public ProjetoService(DataForgeDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProjetoDto>> ObterTodosAsync(
        int idUtilizador)
    {
        return await _context.Projetos
            .AsNoTracking()
            .Where(p =>
                p.IdUtilizador == idUtilizador &&
                p.Ativo)
            .OrderByDescending(p => p.DataCriacao)
            .Select(p => new ProjetoDto
            {
                IdProjeto = p.IdProjeto,
                IdUtilizador = p.IdUtilizador,
                Nome = p.Nome,
                Descricao = p.Descricao,
                Ativo = p.Ativo,
                DataCriacao = p.DataCriacao,
                DataAtualizacao = p.DataAtualizacao
            })
            .ToListAsync();
    }

    public async Task<ProjetoDto?> ObterPorIdAsync(
        int id,
        int idUtilizador)
    {
        return await _context.Projetos
            .AsNoTracking()
            .Where(p =>
                p.IdProjeto == id &&
                p.IdUtilizador == idUtilizador &&
                p.Ativo)
            .Select(p => new ProjetoDto
            {
                IdProjeto = p.IdProjeto,
                IdUtilizador = p.IdUtilizador,
                Nome = p.Nome,
                Descricao = p.Descricao,
                Ativo = p.Ativo,
                DataCriacao = p.DataCriacao,
                DataAtualizacao = p.DataAtualizacao
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ProjetoDto> CriarAsync(
        int idUtilizador,
        CriarProjetoDto dto)
    {
        var utilizadorExiste = await _context.Utilizadores
            .AnyAsync(u =>
                u.IdUtilizador == idUtilizador &&
                u.Ativo);

        if (!utilizadorExiste)
        {
            throw new ArgumentException(
                "O utilizador autenticado não existe ou está inativo."
            );
        }

        var projeto = new Projeto
        {
            IdUtilizador = idUtilizador,
            Nome = dto.Nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(dto.Descricao)
                ? null
                : dto.Descricao.Trim(),
            Ativo = true,
            DataCriacao = DateTime.Now,
            DataAtualizacao = DateTime.Now
        };

        _context.Projetos.Add(projeto);

        await _context.SaveChangesAsync();

        return MapearParaDto(projeto);
    }

    public async Task<ProjetoDto?> AtualizarAsync(
        int id,
        int idUtilizador,
        AtualizarProjetoDto dto)
    {
        var projeto = await _context.Projetos
            .FirstOrDefaultAsync(p =>
                p.IdProjeto == id &&
                p.IdUtilizador == idUtilizador);

        if (projeto is null)
        {
            return null;
        }

        projeto.Nome = dto.Nome.Trim();

        projeto.Descricao =
            string.IsNullOrWhiteSpace(dto.Descricao)
                ? null
                : dto.Descricao.Trim();

        projeto.Ativo = dto.Ativo;
        projeto.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapearParaDto(projeto);
    }

    public async Task<bool> EliminarAsync(
        int id,
        int idUtilizador)
    {
        var projeto = await _context.Projetos
            .FirstOrDefaultAsync(p =>
                p.IdProjeto == id &&
                p.IdUtilizador == idUtilizador &&
                p.Ativo);

        if (projeto is null)
        {
            return false;
        }

        projeto.Ativo = false;
        projeto.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    private static ProjetoDto MapearParaDto(Projeto projeto)
    {
        return new ProjetoDto
        {
            IdProjeto = projeto.IdProjeto,
            IdUtilizador = projeto.IdUtilizador,
            Nome = projeto.Nome,
            Descricao = projeto.Descricao,
            Ativo = projeto.Ativo,
            DataCriacao = projeto.DataCriacao,
            DataAtualizacao = projeto.DataAtualizacao
        };
    }
}
