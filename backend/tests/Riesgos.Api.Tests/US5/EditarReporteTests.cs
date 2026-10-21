using System.Net;
using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Api.Tests.US5;

public class EditarReporteTests
{
    private static readonly ResultadoClasificacion.Exito Original = new(TipoReporte.CondicionInsegura, Categoria.RiesgoElectrico, Criticidad.Alta);
    private static readonly ResultadoClasificacion.Exito Nueva = new(TipoReporte.ActoInseguro, Categoria.UsoEpp, Criticidad.Critica);

    private static Task<HttpResponseMessage> EditarAsync(HttpClient cliente, int id, string? descripcion, string? ubicacion) =>
        cliente.PutAsJsonAsync($"/api/reportes/{id}", new { descripcion, ubicacion });

    // Reporte del empleado ya clasificado con la clasificación Original.
    private static async Task<(ApiFactory App, HttpClient Empleado, int Id)> ReporteClasificadoAsync()
    {
        var app = new ApiFactory();
        app.Clasificador.Resultado = Original;
        var empleado = await app.ClienteAutenticadoAsync();
        var creado = await empleado.CrearReporteAsync();
        await app.ProcesarClasificacionesAsync();
        return (app, empleado, creado.Id);
    }

    private static async Task<string> AsuntoBorradorAsync(ApiFactory app, int id) =>
        (await (await app.ComoAdminAsync()).DetalleJsonAsync(id)).GetProperty("borradorMail").GetProperty("asunto").GetString()!;

    [Fact]
    [Trait("AC", "AC-10")]
    public async Task El_creador_modifica_descripcion_y_ubicacion_de_un_reporte_Abierto()
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;

        var respuesta = await EditarAsync(empleado, id, "Cable cubierto con cinta", "Planta 2");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var detalle = await empleado.DetalleAsync(id);
        Assert.Equal("Cable cubierto con cinta", detalle.Descripcion);
        Assert.Equal("Planta 2", detalle.Ubicacion);
    }

    [Fact]
    [Trait("FR", "FR-017")]
    public async Task La_edicion_responde_de_inmediato_con_la_clasificacion_pendiente()
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;

        var respuesta = await EditarAsync(empleado, id, "Otra descripción", "Planta 2");

        var reporte = await respuesta.Content.ReadFromJsonAsync<ReporteDto>();
        Assert.Equal("Pendiente", reporte!.Criticidad);
        Assert.Equal(1, app.Cola.Cantidad);
        Assert.Contains("Pendiente de clasificación", await AsuntoBorradorAsync(app, id));
    }

    [Theory]
    [Trait("AC", "AC-61")]
    [InlineData("", "Planta 2")]
    [InlineData("Otra descripción", "   ")]
    public async Task Dejar_un_campo_vacio_responde_400_y_el_reporte_no_cambia(string descripcion, string ubicacion)
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;

        var respuesta = await EditarAsync(empleado, id, descripcion, ubicacion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var detalle = await empleado.DetalleAsync(id);
        Assert.Equal(Reportes.Descripcion, detalle.Descripcion);
        Assert.Equal("Alta", detalle.Criticidad);
    }

    [Fact]
    [Trait("AC", "AC-69")]
    [Trait("FR", "FR-019")]
    public async Task Descripcion_de_mas_de_2000_responde_400_no_cambia_y_no_pide_clasificacion()
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;
        var llamadas = app.Clasificador.Entradas.Count;

        var respuesta = await EditarAsync(empleado, id, new string('a', 2001), "Planta 2");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(Reportes.Descripcion, (await empleado.DetalleAsync(id)).Descripcion);
        Assert.Equal(0, app.Cola.Cantidad);
        Assert.Equal(llamadas, app.Clasificador.Entradas.Count);
    }

    [Fact]
    [Trait("AC", "AC-52")]
    [Trait("AC", "AC-57")]
    public async Task Tras_editar_queda_la_nueva_clasificacion_y_el_borrador_la_refleja()
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;
        app.Clasificador.Resultado = Nueva;

        await EditarAsync(empleado, id, "Operario sin guantes", "Taller");
        await app.ProcesarClasificacionesAsync();

        var detalle = await empleado.DetalleAsync(id);
        Assert.Equal("ActoInseguro", detalle.Tipo);
        Assert.Equal("UsoEpp", detalle.Categoria);
        Assert.Equal("Critica", detalle.Criticidad);
        var asunto = await AsuntoBorradorAsync(app, id);
        Assert.Contains("Uso de elementos de protección personal", asunto);
        Assert.Contains("Crítica", asunto);
    }

    [Fact]
    [Trait("AC", "AC-59")]
    [Trait("AC", "AC-62")]
    public async Task Si_falla_la_reclasificacion_la_edicion_se_guarda_y_queda_pendiente()
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;
        app.Clasificador.Resultado = new ResultadoClasificacion.Falla(MotivoFalla.ErrorServicio);

        await EditarAsync(empleado, id, "Nueva descripción", "Planta 1");
        await app.ProcesarClasificacionesAsync();

        var detalle = await empleado.DetalleAsync(id);
        Assert.Equal("Nueva descripción", detalle.Descripcion);
        Assert.Equal("Pendiente", detalle.Tipo);
        Assert.Equal("Pendiente", detalle.Categoria);
        Assert.Equal("Pendiente", detalle.Criticidad);
        var asunto = await AsuntoBorradorAsync(app, id);
        Assert.Contains("Pendiente de clasificación", asunto);
        Assert.DoesNotContain("Alta", asunto);
    }

    [Fact]
    [Trait("FR", "FR-017")]
    public async Task Con_dos_ediciones_seguidas_solo_se_clasifica_la_ultima()
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;
        var llamadas = app.Clasificador.Entradas.Count;
        app.Clasificador.Resultado = Nueva;

        await EditarAsync(empleado, id, "Primera edición", "Planta 1");
        await EditarAsync(empleado, id, "Segunda edición", "Planta 1");
        await app.ProcesarClasificacionesAsync();

        var nuevas = app.Clasificador.Entradas.Skip(llamadas).ToList();
        Assert.Equal("Segunda edición", Assert.Single(nuevas).Descripcion);
        Assert.Equal("Critica", (await empleado.DetalleAsync(id)).Criticidad);
    }

    [Theory]
    [Trait("AC", "AC-11")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task En_EnProgreso_o_Cerrado_responde_409_y_no_cambia(bool cerrar)
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;
        var admin = await app.ComoAdminAsync();
        await admin.PostAsJsonAsync($"/api/admin/reportes/{id}/estado", new { estado = "EnProgreso" });
        if (cerrar)
        {
            await admin.PostAsJsonAsync($"/api/admin/reportes/{id}/estado", new { estado = "Cerrado", accionRealizada = "Se reparó", observacionCierre = "Ok" });
        }

        var respuesta = await EditarAsync(empleado, id, "Cambio tardío", "Planta 9");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(Reportes.Descripcion, (await empleado.DetalleAsync(id)).Descripcion);
    }

    [Fact]
    [Trait("AC", "AC-36")]
    public async Task Otro_empleado_recibe_403_y_el_reporte_no_cambia()
    {
        var (app, empleado, id) = await ReporteClasificadoAsync();
        await using var _ = app;
        var otro = await app.ClienteAutenticadoAsync();

        var respuesta = await EditarAsync(otro, id, "Intruso", "Planta 9");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(Reportes.Descripcion, (await empleado.DetalleAsync(id)).Descripcion);
    }

    [Fact]
    [Trait("FR", "FR-012")]
    public async Task El_administrador_no_edita_reportes_que_no_creo()
    {
        var (app, _, id) = await ReporteClasificadoAsync();
        await using var _ = app;
        var admin = await app.ComoAdminAsync();

        var respuesta = await EditarAsync(admin, id, "Editado por admin", "Planta 9");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-009a")]
    public async Task El_administrador_edita_un_reporte_propio_Abierto()
    {
        await using var app = new ApiFactory();
        var admin = await app.ComoAdminAsync();
        var creado = await admin.CrearReporteAsync();

        var respuesta = await EditarAsync(admin, creado.Id, "Corregido", "Planta 3");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
