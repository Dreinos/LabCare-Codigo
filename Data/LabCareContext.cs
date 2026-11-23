using LabCare.Models;
using Microsoft.EntityFrameworkCore;

namespace LabCare.Data;

public class LabCareContext(DbContextOptions<LabCareContext> options) : DbContext(options)
{
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var equipo = modelBuilder.Entity<Equipo>();
        equipo.ToTable("equipos");
        equipo.Property(x => x.Id).HasColumnName("id");
        equipo.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20);
        equipo.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100);
        equipo.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasMaxLength(100);
        equipo.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        equipo.HasIndex(x => x.Codigo).IsUnique();

        var solicitud = modelBuilder.Entity<Solicitud>();
        solicitud.ToTable("solicitudes");
        solicitud.Property(x => x.Id).HasColumnName("id");
        solicitud.Property(x => x.CodigoSolicitud).HasColumnName("codigo_solicitud").HasMaxLength(25);
        solicitud.Property(x => x.EquipoId).HasColumnName("equipo_id");
        solicitud.Property(x => x.Solicitante).HasColumnName("solicitante").HasMaxLength(100);
        solicitud.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(2000);
        solicitud.Property(x => x.Prioridad).HasColumnName("prioridad").HasMaxLength(5);
        solicitud.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(12).IsConcurrencyToken();
        solicitud.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").HasPrecision(6);
        solicitud.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion")
            .HasPrecision(6).IsConcurrencyToken();
        solicitud.Property(x => x.Activo).HasColumnName("activo").IsConcurrencyToken();
        solicitud.HasIndex(x => x.CodigoSolicitud).IsUnique();
        solicitud.HasOne(x => x.Equipo).WithMany().HasForeignKey(x => x.EquipoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
