using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Persistencia.Configuraciones;

public class ReporteConfig : IEntityTypeConfiguration<Reporte>
{
    public void Configure(EntityTypeBuilder<Reporte> builder)
    {
        builder.Property(r => r.Descripcion).HasMaxLength(Reporte.MaxDescripcion);
        builder.Property(r => r.Ubicacion).HasMaxLength(Reporte.MaxUbicacion);
        builder.Property(r => r.AccionRealizada).HasMaxLength(1000);
        builder.Property(r => r.ObservacionCierre).HasMaxLength(1000);
        builder.Property(r => r.RowVersion).IsConcurrencyToken();

        builder.HasOne(r => r.Creador).WithMany().HasForeignKey(r => r.CreadorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.UsuarioInicioProgreso).WithMany().HasForeignKey(r => r.UsuarioInicioProgresoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.UsuarioCierre).WithMany().HasForeignKey(r => r.UsuarioCierreId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.CreadorId, r.FechaCreacion });
        builder.HasIndex(r => new { r.FechaCreacion, r.Id });
        builder.HasIndex(r => new { r.Estado, r.Criticidad });
    }
}
