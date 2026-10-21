using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US4;

public class AccionesTests
{
    private sealed record AccionDto(int Id, string Descripcion, DateTimeOffset Fecha, string ResponsableEmail);

    private static Task<HttpResponseMessage> RegistrarAsync(HttpClient cliente, int id, string? descripcion) =>
        cliente.PostAsJsonAsync($"/api/admin/reportes/{id}/acciones", new { descripcion });

    private static Task<HttpResponseMessage> CambiarEstadoAsync(HttpClient cliente, int id, string estado, string? accion = null, string? observacion = null) =>
        cliente.PostAsJsonAsync($"/api/admin/reportes/{id}/estado", new { estado, accionRealizada = accion, observacionCierre = observacion });

    private static async Task<List<AccionDto>> AccionesDelDetalleAsync(HttpClient cliente, int id) =>
        (await cliente.DetalleJsonAsync(id)).GetProperty("acciones").Deserialize<List<AccionDto>>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private static async Task<(ApiFactory App, HttpClient Admin, HttpClient Empleado, int Id)> PrepararAsync()
    {
        var app = new ApiFactory();
        var empleado = await app.ClienteAutenticadoAsync();
        var creado = await empleado.CrearReporteAsync();
        return (app, await app.ComoAdminAsync(), empleado, creado.Id);
    }

    [Fact]
    [Trait("AC", "AC-24")]
    [Trait("AC", "AC-54")]
    public async Task La_accion_queda_con_la_fecha_del_registro_y_el_administrador_como_responsable()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;
        await CambiarEstadoAsync(admin, id, "EnProgreso");

        var respuesta = await RegistrarAsync(admin, id, "Se señalizó la zona");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var accion = Assert.Single(await AccionesDelDetalleAsync(admin, id));
        Assert.Equal("Se señalizó la zona", accion.Descripcion);
        Assert.Equal(app.Reloj.GetUtcNow(), accion.Fecha);
        Assert.Equal(ApiFactory.AdminEmail, accion.ResponsableEmail);
    }

    [Fact]
    [Trait("FR", "FR-028")]
    public async Task Se_pueden_registrar_acciones_en_un_reporte_Abierto_y_se_listan_por_fecha()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;

        await RegistrarAsync(admin, id, "Primera");
        app.Reloj.Advance(TimeSpan.FromMinutes(5));
        await RegistrarAsync(admin, id, "Segunda");

        Assert.Equal(["Primera", "Segunda"], (await AccionesDelDetalleAsync(admin, id)).Select(a => a.Descripcion));
    }

    [Fact]
    [Trait("AC", "AC-55")]
    public async Task En_un_reporte_Cerrado_responde_409_y_no_registra_la_accion()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;
        await CambiarEstadoAsync(admin, id, "EnProgreso");
        await CambiarEstadoAsync(admin, id, "Cerrado", "Se reparó", "Ok");

        var respuesta = await RegistrarAsync(admin, id, "Tarde");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Empty(await AccionesDelDetalleAsync(admin, id));
    }

    [Theory]
    [Trait("FR", "FR-028")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Descripcion_vacia_responde_400(string? descripcion)
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await RegistrarAsync(admin, id, descripcion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-028")]
    public async Task Descripcion_de_mas_de_1000_caracteres_responde_400()
    {
        var (app, admin, _, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await RegistrarAsync(admin, id, new string('a', 1001));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-37")]
    public async Task Un_empleado_recibe_403()
    {
        var (app, _, empleado, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await RegistrarAsync(empleado, id, "No debería");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-011")]
    public async Task El_empleado_creador_ve_las_acciones_en_su_detalle()
    {
        var (app, admin, empleado, id) = await PrepararAsync();
        await using var _ = app;
        await RegistrarAsync(admin, id, "Se señalizó la zona");

        Assert.Equal("Se señalizó la zona", Assert.Single(await AccionesDelDetalleAsync(empleado, id)).Descripcion);
    }
}
