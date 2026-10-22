using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Persistencia.Configuraciones;

public class EventoNotificacionConfig : IEntityTypeConfiguration<EventoNotificacion>
{
    public void Configure(EntityTypeBuilder<EventoNotificacion> builder)
    {
        // Un único evento por reporte (AC-07).
        builder.HasIndex(e => e.ReporteId).IsUnique();
        builder.HasOne(e => e.Reporte).WithOne().HasForeignKey<EventoNotificacion>(e => e.ReporteId).OnDelete(DeleteBehavior.Cascade);
    }
}
