using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US4;

public class EstadoTests
{
    private static Task<HttpResponseMessage> CambiarEstadoAsync(HttpClient cliente, int id, string estado, string? accionRealizada = null, string? observacionCierre = null) =>
        cliente.PostAsJsonAsync($"/api/admin/reportes/{id}/estado", new { estado, accionRealizada, observacionCierre });

    private static async Task<(ApiFactory App, HttpClient Admin, HttpClient Empleado, int Id)> PrepararAsync()
    {
        var app = new ApiFactory();
        var empleado = await app.ClienteAutenticadoAsync();
        var creado = await empleado.CrearReporteAsync();
        return (app, await app.ComoAdminAsync(), empleado, creado.Id);
    }

    [Fact]
    [Trait("AC", "AC-15")]
    public async Task De_Abierto_a_EnProgreso_queda_registrado()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await CambiarEstadoAsync(admin, id, "EnProgreso");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = await admin.DetalleJsonAsync(id);
        Assert.Equal("EnProgreso", detalle.GetProperty("estado").GetString());
        Assert.Equal(ApiFactory.AdminEmail, detalle.GetProperty("inicioProgreso").GetProperty("usuarioEmail").GetString());
    }

    [Fact]
    [Trait("AC", "AC-34")]
    public async Task Cerrar_directamente_desde_Abierto_responde_409_y_sigue_Abierto()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await CambiarEstadoAsync(admin, id, "Cerrado", "Se reparó", "Ok");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("Abierto", (await admin.DetalleAsync(id)).Estado);
    }

    [Theory]
    [Trait("AC", "AC-33")]
    [InlineData("Abierto")]
    [InlineData("EnProgreso")]
    public async Task Un_reporte_Cerrado_no_puede_reabrirse_ni_volver_a_EnProgreso(string destino)
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;
        await CambiarEstadoAsync(admin, id, "EnProgreso");
        await CambiarEstadoAsync(admin, id, "Cerrado", "Se reparó", "Ok");

        var respuesta = await CambiarEstadoAsync(admin, id, destino);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("Cerrado", (await admin.DetalleAsync(id)).Estado);
    }

    [Fact]
    [Trait("FR", "FR-027")]
    public async Task De_EnProgreso_no_se_puede_volver_a_Abierto()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;
        await CambiarEstadoAsync(admin, id, "EnProgreso");

        var respuesta = await CambiarEstadoAsync(admin, id, "Abierto");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Theory]
    [Trait("AC", "AC-25")]
    [InlineData(null, "Ok")]
    [InlineData("Se reparó", null)]
    [InlineData("  ", "Ok")]
    public async Task Cerrar_sin_accion_realizada_u_observacion_responde_400_y_sigue_EnProgreso(string? accion, string? observacion)
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;
        await CambiarEstadoAsync(admin, id, "EnProgreso");

        var respuesta = await CambiarEstadoAsync(admin, id, "Cerrado", accion, observacion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("EnProgreso", (await admin.DetalleAsync(id)).Estado);
    }

    [Fact]
    [Trait("AC", "AC-35")]
    [Trait("AC", "AC-18")]
    [Trait("FR", "FR-029")]
    public async Task Cerrar_con_ambos_textos_y_sin_acciones_previas_registra_el_cierre_visible_para_ambos_roles()
    {
        var (app, admin, empleado, id) = await PrepararAsync();
        await using var _ = app;
        await CambiarEstadoAsync(admin, id, "EnProgreso");
        app.Reloj.Advance(TimeSpan.FromHours(2));

        var respuesta = await CambiarEstadoAsync(admin, id, "Cerrado", "Se reemplazó el cable", "Se verificó la instalación");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        foreach (var cliente in new[] { admin, empleado })
        {
            var cierre = (await cliente.DetalleJsonAsync(id)).GetProperty("cierre");
            Assert.Equal("Se reemplazó el cable", cierre.GetProperty("accionRealizada").GetString());
            Assert.Equal("Se verificó la instalación", cierre.GetProperty("observacionCierre").GetString());
            Assert.Equal(ApiFactory.AdminEmail, cierre.GetProperty("usuarioEmail").GetString());
            Assert.Equal(app.Reloj.GetUtcNow(), cierre.GetProperty("fecha").GetDateTimeOffset());
        }
    }

    [Fact]
    [Trait("FR", "FR-027")]
    [Trait("FR", "FR-017")]
    public async Task Un_reporte_pendiente_de_clasificacion_cambia_de_estado_sin_encolar_clasificaciones()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;
        await app.ProcesarSinRespuestaAsync();
        Assert.Equal("Pendiente", (await admin.DetalleAsync(id)).Criticidad);

        var aEnProgreso = await CambiarEstadoAsync(admin, id, "EnProgreso");
        var aCerrado = await CambiarEstadoAsync(admin, id, "Cerrado", "Se reparó", "Ok");

        Assert.Equal(HttpStatusCode.OK, aEnProgreso.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aCerrado.StatusCode);
        Assert.Equal(0, app.Cola.Cantidad);
    }

    [Fact]
    [Trait("AC", "AC-16")]
    public async Task Un_empleado_recibe_403_al_cambiar_el_estado()
    {
        var (app, _, empleado, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await CambiarEstadoAsync(empleado, id, "EnProgreso");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Estado_desconocido_responde_400_y_reporte_inexistente_404()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;

        var desconocido = await CambiarEstadoAsync(admin, id, "Archivado");
        var inexistente = await CambiarEstadoAsync(admin, 9999, "EnProgreso");

        Assert.Equal(HttpStatusCode.BadRequest, desconocido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-027")]
    public async Task Dos_cambios_simultaneos_dejan_uno_OK_y_el_otro_409()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;
        var otroAdmin = await app.ComoAdminAsync();

        var respuestas = await Task.WhenAll(CambiarEstadoAsync(admin, id, "EnProgreso"), CambiarEstadoAsync(otroAdmin, id, "EnProgreso"));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
    }
}
