using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Riesgos.Api.Tests.Infraestructura;
using Riesgos.Core.Dominio;

namespace Riesgos.Api.Tests.US3;

public class PanelTests
{
    private sealed record PanelDto(int CriticosAbiertos, int PendientesAbiertos);

    // Deja el reporte con la criticidad y el estado indicados directamente en la base.
    private static void Ajustar(ApiFactory app, int id, Criticidad criticidad, EstadoReporte estado) =>
        app.EnScope(db => db.Reportes.Where(r => r.Id == id).ExecuteUpdate(s => s
            .SetProperty(r => r.Criticidad, criticidad)
            .SetProperty(r => r.Estado, estado)));

    [Fact]
    [Trait("AC", "AC-26")]
    [Trait("AC", "AC-60")]
    [Trait("FR", "FR-026")]
    public async Task Cuenta_solo_los_reportes_en_estado_Abierto()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        var ids = new List<int>();
        for (var i = 0; i < 7; i++)
        {
            ids.Add((await cliente.CrearReporteAsync($"Reporte {i}")).Id);
        }

        Ajustar(app, ids[0], Criticidad.Critica, EstadoReporte.Abierto);
        Ajustar(app, ids[1], Criticidad.Critica, EstadoReporte.Abierto);
        Ajustar(app, ids[2], Criticidad.Critica, EstadoReporte.EnProgreso);
        Ajustar(app, ids[3], Criticidad.Critica, EstadoReporte.Cerrado);
        Ajustar(app, ids[4], Criticidad.Alta, EstadoReporte.Abierto);
        Ajustar(app, ids[5], Criticidad.Pendiente, EstadoReporte.Abierto);
        Ajustar(app, ids[6], Criticidad.Pendiente, EstadoReporte.EnProgreso);
        var admin = await app.ComoAdminAsync();

        var panel = await admin.GetFromJsonAsync<PanelDto>("/api/admin/panel");

        Assert.Equal(2, panel!.CriticosAbiertos);
        Assert.Equal(1, panel.PendientesAbiertos);
    }

    [Fact]
    [Trait("FR", "FR-033")]
    public async Task Sin_reportes_los_contadores_son_cero()
    {
        await using var app = new ApiFactory();
        var admin = await app.ComoAdminAsync();

        var panel = await admin.GetFromJsonAsync<PanelDto>("/api/admin/panel");

        Assert.Equal(new PanelDto(0, 0), panel);
    }

    [Fact]
    [Trait("AC", "AC-38")]
    public async Task Un_empleado_recibe_403()
    {
        await using var app = new ApiFactory();
        var empleado = await app.ClienteAutenticadoAsync();

        var respuesta = await empleado.GetAsync("/api/admin/panel");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
