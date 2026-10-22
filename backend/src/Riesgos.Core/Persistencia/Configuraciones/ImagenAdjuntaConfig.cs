using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Persistencia.Configuraciones;

public class ImagenAdjuntaConfig : IEntityTypeConfiguration<ImagenAdjunta>
{
    public void Configure(EntityTypeBuilder<ImagenAdjunta> builder)
    {
        builder.ToTable("ImagenesAdjuntas");
        builder.Property(i => i.NombreArchivo).HasMaxLength(64);
        builder.Property(i => i.TipoContenido).HasMaxLength(20);
        builder.HasOne(i => i.Reporte).WithMany(r => r.Imagenes).HasForeignKey(i => i.ReporteId).OnDelete(DeleteBehavior.Cascade);
    }
}
