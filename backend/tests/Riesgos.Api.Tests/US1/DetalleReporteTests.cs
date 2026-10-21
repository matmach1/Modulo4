using System.Net;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US1;

public class DetalleReporteTests
{
    [Fact]
    [Trait("AC", "AC-50")]
    [Trait("AC", "AC-17")]
    public async Task El_creador_ve_su_reporte_con_fecha_hora_y_usuario_de_creacion()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        var cliente = await app.ClienteAutenticadoAsync(email);
        var creado = await cliente.CrearReporteAsync();

        var respuesta = await cliente.GetAsync($"/api/reportes/{creado.Id}");
        var detalle = await cliente.DetalleAsync(creado.Id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(app.Reloj.GetUtcNow(), detalle.FechaCreacion);
        Assert.Equal(email, detalle.CreadorEmail);
    }

    [Fact]
    [Trait("AC", "AC-04")]
    public async Task Otro_empleado_recibe_403_y_no_ve_el_reporte()
    {
        await using var app = new ApiFactory();
        var a = await app.ClienteAutenticadoAsync();
        var b = await app.ClienteAutenticadoAsync();
        var creado = await a.CrearReporteAsync();

        var respuesta = await b.GetAsync($"/api/reportes/{creado.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.DoesNotContain(Reportes.Descripcion, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Reporte_inexistente_responde_404()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.GetAsync("/api/reportes/9999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-011")]
    [Trait("AC", "AC-39")]
    public async Task El_detalle_del_empleado_no_incluye_accion_sugerida_ni_borrador()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        var creado = await cliente.CrearReporteAsync();

        var json = await cliente.DetalleJsonAsync(creado.Id);

        Assert.False(json.TryGetProperty("accionSugerida", out _));
        Assert.False(json.TryGetProperty("borradorMail", out _));
    }
}
