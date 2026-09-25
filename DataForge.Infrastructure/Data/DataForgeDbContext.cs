using System;
using System.Collections.Generic;
using DataForge.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DataForge.Infrastructure.Data;

public partial class DataForgeDbContext : DbContext
{
    public DataForgeDbContext(DbContextOptions<DataForgeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Analise> Analises { get; set; }

    public virtual DbSet<Anomalias> Anomaliases { get; set; }

    public virtual DbSet<Dataset> Datasets { get; set; }

    public virtual DbSet<DatasetColuna> DatasetColunas { get; set; }

    public virtual DbSet<EstatisticasColuna> EstatisticasColunas { get; set; }

    public virtual DbSet<Historico> Historicos { get; set; }

    public virtual DbSet<Importaco> Importacoes { get; set; }

    public virtual DbSet<ProblemasQualidade> ProblemasQualidades { get; set; }

    public virtual DbSet<Projeto> Projetos { get; set; }

    public virtual DbSet<Transformaco> Transformacoes { get; set; }

    public virtual DbSet<Utilizadore> Utilizadores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Analise>(entity =>
        {
            entity.HasKey(e => e.IdAnalise);

            entity.HasIndex(e => e.IdDataset, "IX_Analises_IdDataset");

            entity.HasIndex(e => e.IdUtilizador, "IX_Analises_IdUtilizador");

            entity.Property(e => e.DataCriacao).HasDefaultValueSql("(sysdatetime())", "DF_Analises_DataCriacao");
            entity.Property(e => e.Nome).HasMaxLength(200);
            entity.Property(e => e.Tipo).HasMaxLength(100);

            entity.HasOne(d => d.IdDatasetNavigation).WithMany(p => p.Analises)
                .HasForeignKey(d => d.IdDataset)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Analises_Datasets");

            entity.HasOne(d => d.IdUtilizadorNavigation).WithMany(p => p.Analises)
                .HasForeignKey(d => d.IdUtilizador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Analises_Utilizadores");
        });

        modelBuilder.Entity<Anomalias>(entity =>
        {
            entity.HasKey(e => e.IdAnomalia);

            entity.ToTable("Anomalias");

            entity.HasIndex(e => e.IdColuna, "IX_Anomalias_IdColuna");

            entity.HasIndex(e => e.IdDataset, "IX_Anomalias_IdDataset");

            entity.HasIndex(e => e.Ignorada, "IX_Anomalias_Ignorada");

            entity.Property(e => e.DataDeteccao).HasDefaultValueSql("(sysdatetime())", "DF_Anomalias_DataDeteccao");
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Score).HasColumnType("decimal(10, 6)");
            entity.Property(e => e.Tipo).HasMaxLength(100);

            entity.HasOne(d => d.IdColunaNavigation).WithMany(p => p.Anomaliases)
                .HasForeignKey(d => d.IdColuna)
                .HasConstraintName("FK_Anomalias_Colunas");

            entity.HasOne(d => d.IdDatasetNavigation).WithMany(p => p.Anomaliases)
                .HasForeignKey(d => d.IdDataset)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Anomalias_Datasets");
        });

        modelBuilder.Entity<Dataset>(entity =>
        {
            entity.HasKey(e => e.IdDataset);

            entity.HasIndex(e => e.DataImportacao, "IX_Datasets_DataImportacao").IsDescending();

            entity.HasIndex(e => e.Estado, "IX_Datasets_Estado");

            entity.HasIndex(e => e.IdProjeto, "IX_Datasets_IdProjeto");

            entity.Property(e => e.CaminhoArquivo).HasMaxLength(1000);
            entity.Property(e => e.DataImportacao).HasDefaultValueSql("(sysdatetime())", "DF_Datasets_DataImportacao");
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Pendente", "DF_Datasets_Estado");
            entity.Property(e => e.Nome).HasMaxLength(255);
            entity.Property(e => e.NomeOriginal).HasMaxLength(255);
            entity.Property(e => e.QualityScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TipoArquivo).HasMaxLength(20);

            entity.HasOne(d => d.IdProjetoNavigation).WithMany(p => p.Datasets)
                .HasForeignKey(d => d.IdProjeto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Datasets_Projetos");
        });

        modelBuilder.Entity<DatasetColuna>(entity =>
        {
            entity.HasKey(e => e.IdColuna);

            entity.HasIndex(e => e.IdDataset, "IX_DatasetColunas_IdDataset");

            entity.HasIndex(e => new { e.IdDataset, e.Nome }, "IX_DatasetColunas_Nome");

            entity.HasIndex(e => new { e.IdDataset, e.Posicao }, "UQ_DatasetColunas_Dataset_Posicao").IsUnique();

            entity.Property(e => e.Nome).HasMaxLength(255);
            entity.Property(e => e.PercentualValido).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TipoDetectado).HasMaxLength(50);
            entity.Property(e => e.TipoOriginal).HasMaxLength(100);

            entity.HasOne(d => d.IdDatasetNavigation).WithMany(p => p.DatasetColunas)
                .HasForeignKey(d => d.IdDataset)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatasetColunas_Datasets");
        });

        modelBuilder.Entity<EstatisticasColuna>(entity =>
        {
            entity.HasKey(e => e.IdEstatistica);

            entity.HasIndex(e => e.IdColuna, "IX_EstatisticasColunas_IdColuna");

            entity.Property(e => e.DataCalculo).HasDefaultValueSql("(sysdatetime())", "DF_EstatisticasColunas_DataCalculo");
            entity.Property(e => e.DesvioPadrao).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.Maximo).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.Media).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.Mediana).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.Minimo).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.P25).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.P50).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.P75).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.P90).HasColumnType("decimal(38, 10)");
            entity.Property(e => e.P99).HasColumnType("decimal(38, 10)");

            entity.HasOne(d => d.IdColunaNavigation).WithMany(p => p.EstatisticasColunas)
                .HasForeignKey(d => d.IdColuna)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstatisticasColunas_Colunas");
        });

        modelBuilder.Entity<Historico>(entity =>
        {
            entity.HasKey(e => e.IdHistorico);

            entity.ToTable("Historico");

            entity.HasIndex(e => e.DataAcao, "IX_Historico_DataAcao").IsDescending();

            entity.HasIndex(e => e.IdDataset, "IX_Historico_IdDataset");

            entity.HasIndex(e => e.IdProjeto, "IX_Historico_IdProjeto");

            entity.HasIndex(e => e.IdUtilizador, "IX_Historico_IdUtilizador");

            entity.Property(e => e.Acao).HasMaxLength(150);
            entity.Property(e => e.DataAcao).HasDefaultValueSql("(sysdatetime())", "DF_Historico_DataAcao");
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Entidade).HasMaxLength(100);

            entity.HasOne(d => d.IdDatasetNavigation).WithMany(p => p.Historicos)
                .HasForeignKey(d => d.IdDataset)
                .HasConstraintName("FK_Historico_Datasets");

            entity.HasOne(d => d.IdProjetoNavigation).WithMany(p => p.Historicos)
                .HasForeignKey(d => d.IdProjeto)
                .HasConstraintName("FK_Historico_Projetos");

            entity.HasOne(d => d.IdUtilizadorNavigation).WithMany(p => p.Historicos)
                .HasForeignKey(d => d.IdUtilizador)
                .HasConstraintName("FK_Historico_Utilizadores");
        });

        modelBuilder.Entity<Importaco>(entity =>
        {
            entity.HasKey(e => e.IdImportacao);

            entity.HasIndex(e => e.Estado, "IX_Importacoes_Estado");

            entity.HasIndex(e => e.IdDataset, "IX_Importacoes_IdDataset");

            entity.HasIndex(e => e.IdUtilizador, "IX_Importacoes_IdUtilizador");

            entity.Property(e => e.DataCriacao).HasDefaultValueSql("(sysdatetime())", "DF_Importacoes_DataCriacao");
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Pendente", "DF_Importacoes_Estado");

            entity.HasOne(d => d.IdDatasetNavigation).WithMany(p => p.Importacos)
                .HasForeignKey(d => d.IdDataset)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Importacoes_Datasets");

            entity.HasOne(d => d.IdUtilizadorNavigation).WithMany(p => p.Importacos)
                .HasForeignKey(d => d.IdUtilizador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Importacoes_Utilizadores");
        });

        modelBuilder.Entity<ProblemasQualidade>(entity =>
        {
            entity.HasKey(e => e.IdProblema);

            entity.ToTable("ProblemasQualidade");

            entity.HasIndex(e => e.IdColuna, "IX_ProblemasQualidade_IdColuna");

            entity.HasIndex(e => e.IdDataset, "IX_ProblemasQualidade_IdDataset");

            entity.HasIndex(e => e.Resolvido, "IX_ProblemasQualidade_Resolvido");

            entity.HasIndex(e => e.Tipo, "IX_ProblemasQualidade_Tipo");

            entity.Property(e => e.DataDeteccao).HasDefaultValueSql("(sysdatetime())", "DF_ProblemasQualidade_DataDeteccao");
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Severidade)
                .HasMaxLength(30)
                .HasDefaultValue("Aviso", "DF_ProblemasQualidade_Severidade");
            entity.Property(e => e.Tipo).HasMaxLength(100);

            entity.HasOne(d => d.IdColunaNavigation).WithMany(p => p.ProblemasQualidades)
                .HasForeignKey(d => d.IdColuna)
                .HasConstraintName("FK_ProblemasQualidade_Colunas");

            entity.HasOne(d => d.IdDatasetNavigation).WithMany(p => p.ProblemasQualidades)
                .HasForeignKey(d => d.IdDataset)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProblemasQualidade_Datasets");
        });

        modelBuilder.Entity<Projeto>(entity =>
        {
            entity.HasKey(e => e.IdProjeto);

            entity.HasIndex(e => e.Ativo, "IX_Projetos_Ativo");

            entity.HasIndex(e => e.IdUtilizador, "IX_Projetos_IdUtilizador");

            entity.Property(e => e.Ativo).HasDefaultValue(true, "DF_Projetos_Ativo");
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("(sysdatetime())", "DF_Projetos_DataCriacao");
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Nome).HasMaxLength(200);

            entity.HasOne(d => d.IdUtilizadorNavigation).WithMany(p => p.Projetos)
                .HasForeignKey(d => d.IdUtilizador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Projetos_Utilizadores");
        });

        modelBuilder.Entity<Transformaco>(entity =>
        {
            entity.HasKey(e => e.IdTransformacao);

            entity.HasIndex(e => e.IdColuna, "IX_Transformacoes_IdColuna");

            entity.HasIndex(e => e.IdDataset, "IX_Transformacoes_IdDataset");

            entity.HasIndex(e => e.IdUtilizador, "IX_Transformacoes_IdUtilizador");

            entity.Property(e => e.DataCriacao).HasDefaultValueSql("(sysdatetime())", "DF_Transformacoes_DataCriacao");
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasDefaultValue("Pendente", "DF_Transformacoes_Estado");
            entity.Property(e => e.Tipo).HasMaxLength(100);

            entity.HasOne(d => d.IdColunaNavigation).WithMany(p => p.Transformacos)
                .HasForeignKey(d => d.IdColuna)
                .HasConstraintName("FK_Transformacoes_Colunas");

            entity.HasOne(d => d.IdDatasetNavigation).WithMany(p => p.Transformacos)
                .HasForeignKey(d => d.IdDataset)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transformacoes_Datasets");

            entity.HasOne(d => d.IdUtilizadorNavigation).WithMany(p => p.Transformacos)
                .HasForeignKey(d => d.IdUtilizador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transformacoes_Utilizadores");
        });

        modelBuilder.Entity<Utilizadore>(entity =>
        {
            entity.HasKey(e => e.IdUtilizador);

            entity.HasIndex(e => e.Email, "UQ_Utilizadores_Email").IsUnique();

            entity.Property(e => e.Ativo).HasDefaultValue(true, "DF_Utilizadores_Ativo");
            entity.Property(e => e.DataCriacao).HasDefaultValueSql("(sysdatetime())", "DF_Utilizadores_DataCriacao");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FotoPerfil).HasMaxLength(500);
            entity.Property(e => e.Nome).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Perfil)
                .HasMaxLength(50)
                .HasDefaultValue("Analista", "DF_Utilizadores_Perfil");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

