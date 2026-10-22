using Microsoft.EntityFrameworkCore;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US3;

public class EventoNotificacionTests
{
    [Fact]
    [Trait("AC", "AC-07")]
    [Trait("FR", "FR-021")]
    public async Task Al_terminar_la_creacion_existe_un_unico_evento_asociado_al_reporte()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var creado = await cliente.CrearReporteAsync();
        await app.ProcesarClasificacionesAsync();

        var eventos = app.EnScope(db => db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM EventosNotificacion WHERE ReporteId = {0}", creado.Id)
            .Single());
        Assert.Equal(1, eventos);
    }
}
