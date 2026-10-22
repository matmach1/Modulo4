using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Persistencia.Configuraciones;

public class AccionCorrectivaConfig : IEntityTypeConfiguration<AccionCorrectiva>
{
    public void Configure(EntityTypeBuilder<AccionCorrectiva> builder)
    {
        builder.ToTable("AccionesCorrectivas");
        builder.Property(a => a.Descripcion).HasMaxLength(Reporte.MaxTextoGestion);
        builder.HasOne(a => a.Reporte).WithMany(r => r.Acciones).HasForeignKey(a => a.ReporteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Responsable).WithMany().HasForeignKey(a => a.ResponsableId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.ReporteId, a.Fecha });
    }
}
