using System.Text;
using DataForge.Application.Interfaces;
using DataForge.Infrastructure.Data;
using DataForge.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// DATABASE
var connectionString = builder.Configuration
    .GetConnectionString("DataForgeConnection")
    ?? throw new InvalidOperationException(
        "A connection string 'DataForgeConnection' não foi encontrada."
    );

builder.Services.AddDbContext<DataForgeDbContext>(options =>
    options.UseSqlServer(connectionString));

// SERVICES
builder.Services.AddScoped<IProjetoService, ProjetoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDatasetService, DatasetService>();
builder.Services.AddScoped<IDatasetProcessingService, DatasetProcessingService>();
builder.Services.AddScoped<IDataQualityService, DataQualityService>();
builder.Services.AddScoped<IQualityScoreService, QualityScoreService>();
builder.Services.AddScoped<IDatasetStatisticsService, DatasetStatisticsService>();
builder.Services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
builder.Services.AddScoped<IOutlierDetectionService, OutlierDetectionService>();
builder.Services.AddScoped<IDatasetPreviewService, DatasetPreviewService>();
builder.Services.AddScoped<IExplorationService, ExplorationService>();
builder.Services.AddScoped<IColumnSummaryService, ColumnSummaryService>();
builder.Services.AddScoped<IDatasetExplorationOverviewService, DatasetExplorationOverviewService>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "A chave JWT não foi configurada."
    );

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? "DataForge.Api";

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? "DataForge.Frontend";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ClockSkew = TimeSpan.Zero
            };
    });

// CONTROLLERS
builder.Services.AddControllers();

// OPENAPI
builder.Services.AddOpenApi();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// PROFILE DATABASE PATCH
// Mantem compatibilidade com a base SQL existente sem
// alterar as tabelas geradas pelo Scaffold-DbContext.
using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<DataForgeDbContext>();

    await db.Database.ExecuteSqlRawAsync("""
        IF COL_LENGTH('Utilizadores', 'FotoPerfil') IS NULL
        BEGIN
            ALTER TABLE Utilizadores
            ADD FotoPerfil nvarchar(500) NULL;
        END
        """);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();












