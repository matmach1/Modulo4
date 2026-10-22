using System.Net;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US6;

public class VencimientoSesionTests
{
    [Fact]
    [Trait("AC", "AC-48")]
    public async Task Despues_de_24_horas_sin_actividad_responde_401()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        app.Reloj.Advance(TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1));
        var respuesta = await cliente.GetAsync("/api/reportes/mios");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-005")]
    public async Task La_actividad_renueva_el_plazo_y_no_hay_duracion_maxima()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        // Tres días seguidos de uso, cada uno a las 23 h del anterior.
        for (var dia = 0; dia < 3; dia++)
        {
            app.Reloj.Advance(TimeSpan.FromHours(23));
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/reportes/mios")).StatusCode);
        }
    }

    [Fact]
    [Trait("FR", "FR-005")]
    public async Task Una_sesion_vencida_no_vuelve_a_valer()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        app.Reloj.Advance(TimeSpan.FromHours(25));
        await cliente.GetAsync("/api/auth/yo");

        Assert.Equal(0, app.EnScope(db => db.Sesiones.Count()));
    }
}
