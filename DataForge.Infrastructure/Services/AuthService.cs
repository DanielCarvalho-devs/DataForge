using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DataForge.Application.DTOs.Auth;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DataForge.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly DataForgeDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        DataForgeDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegistarAsync(
        RegistarUtilizadorDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var emailExiste = await _context.Utilizadores
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExiste)
        {
            throw new ArgumentException(
                "Já existe um utilizador com este email."
            );
        }

        var utilizador = new Utilizadore
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            PasswordHash = CriarPasswordHash(dto.Password),
            Perfil = "Analista",
            Ativo = true,
            DataCriacao = DateTime.Now
        };

        _context.Utilizadores.Add(utilizador);

        await _context.SaveChangesAsync();

        return CriarResposta(utilizador);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var utilizador = await _context.Utilizadores
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email &&
                u.Ativo
            );

        if (utilizador is null)
        {
            return null;
        }

        if (!VerificarPassword(
            dto.Password,
            utilizador.PasswordHash))
        {
            return null;
        }

        utilizador.UltimoAcesso = DateTime.Now;

        await _context.SaveChangesAsync();

        return CriarResposta(utilizador);
    }

    private AuthResponseDto CriarResposta(Utilizadore utilizador)
    {
        var expiracao = DateTime.UtcNow.AddHours(8);

        return new AuthResponseDto
        {
            Token = CriarToken(utilizador, expiracao),

            ExpiraEm = expiracao,

            Utilizador = new UtilizadorDto
            {
                IdUtilizador = utilizador.IdUtilizador,
                Nome = utilizador.Nome,
                Email = utilizador.Email,
                Perfil = utilizador.Perfil,
                Ativo = utilizador.Ativo,
                DataCriacao = utilizador.DataCriacao,
                UltimoAcesso = utilizador.UltimoAcesso
            }
        };
    }

    private string CriarToken(
        Utilizadore utilizador,
        DateTime expiracao)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "A chave JWT não foi configurada."
            );

        var issuer = _configuration["Jwt:Issuer"]
            ?? "DataForge.Api";

        var audience = _configuration["Jwt:Audience"]
            ?? "DataForge.Frontend";

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                utilizador.IdUtilizador.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                utilizador.Nome
            ),

            new Claim(
                ClaimTypes.Email,
                utilizador.Email
            ),

            new Claim(
                ClaimTypes.Role,
                utilizador.Perfil
            )
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiracao,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private static string CriarPasswordHash(string password)
    {
        const int iteracoes = 100000;

        var salt = RandomNumberGenerator.GetBytes(16);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iteracoes,
            HashAlgorithmName.SHA256,
            32
        );

        return $"{iteracoes}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerificarPassword(
        string password,
        string passwordHash)
    {
        try
        {
            var partes = passwordHash.Split('.');

            if (partes.Length != 3)
            {
                return false;
            }

            var iteracoes = int.Parse(partes[0]);
            var salt = Convert.FromBase64String(partes[1]);
            var hashEsperado = Convert.FromBase64String(partes[2]);

            var hashInformado = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                hashEsperado.Length
            );

            return CryptographicOperations.FixedTimeEquals(
                hashInformado,
                hashEsperado
            );
        }
        catch
        {
            return false;
        }
    }
}

