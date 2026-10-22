using System.Net;
using System.Text.Json;
using Riesgos.Api.Tests.Infraestructura;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Api.Tests.US3;

public class DetalleAdminTests
{
    private static async Task<(ApiFactory App, HttpClient Admin, int Id, string EmailCreador)> ReporteClasificadoAsync(
        Criticidad criticidad, Categoria categoria = Categoria.RiesgoElectrico, string? sugerida = null)
    {
        var app = new ApiFactory();
        app.Clasificador.Resultado = new ResultadoClasificacion.Exito(TipoReporte.CondicionInsegura, categoria, criticidad);
        var email = Clientes.EmailNuevo();
        var empleado = await app.ClienteAutenticadoAsync(email);
        var creado = await empleado.CrearReporteAsync(categoriaSugerida: sugerida);
        await app.ProcesarClasificacionesAsync();
        return (app, await app.ComoAdminAsync(), creado.Id, email);
    }

    private static string? AccionSugerida(JsonElement json) =>
        json.TryGetProperty("accionSugerida", out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;

    [Fact]
    [Trait("AC", "AC-51")]
    public async Task El_administrador_ve_el_detalle_de_cualquier_reporte()
    {
        var (app, admin, id, _) = await ReporteClasificadoAsync(Criticidad.Media);
        await using var _ = app;

        var respuesta = await admin.GetAsync($"/api/reportes/{id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [Trait("AC", "AC-08")]
    [Trait("AC", "AC-09")]
    [Trait("AC", "AC-30")]
    [InlineData(Criticidad.Critica, "Detener actividad")]
    [InlineData(Criticidad.Alta, "Requiere intervención inmediata")]
    [InlineData(Criticidad.Media, null)]
    [InlineData(Criticidad.Baja, null)]
    public async Task Muestra_la_accion_sugerida_segun_la_criticidad(Criticidad criticidad, string? esperada)
    {
        var (app, admin, id, _) = await ReporteClasificadoAsync(criticidad);
        await using var _ = app;

        Assert.Equal(esperada, AccionSugerida(await admin.DetalleJsonAsync(id)));
    }

    [Fact]
    [Trait("AC", "AC-30")]
    public async Task Con_clasificacion_pendiente_no_hay_accion_sugerida()
    {
        await using var app = new ApiFactory();
        var empleado = await app.ClienteAutenticadoAsync();
        var creado = await empleado.CrearReporteAsync();
        var admin = await app.ComoAdminAsync();

        Assert.Null(AccionSugerida(await admin.DetalleJsonAsync(creado.Id)));
    }

    [Fact]
    [Trait("AC", "AC-41")]
    public async Task El_borrador_tiene_categoria_y_criticidad_en_el_asunto_y_los_datos_en_el_cuerpo()
    {
        var (app, admin, id, email) = await ReporteClasificadoAsync(Criticidad.Alta);
        await using var _ = app;

        var borrador = (await admin.DetalleJsonAsync(id)).GetProperty("borradorMail");
        var asunto = borrador.GetProperty("asunto").GetString()!;
        var cuerpo = borrador.GetProperty("cuerpo").GetString()!;

        Assert.Contains("Riesgo eléctrico", asunto);
        Assert.Contains("Alta", asunto);
        Assert.Contains(Reportes.Descripcion, cuerpo);
        Assert.Contains(Reportes.Ubicacion, cuerpo);
        Assert.Contains(email, cuerpo);
        Assert.Contains("21/10/2026", cuerpo);
    }

    [Fact]
    [Trait("AC", "AC-43")]
    public async Task Con_categorias_distintas_el_administrador_ve_ambas()
    {
        var (app, admin, id, _) = await ReporteClasificadoAsync(Criticidad.Media, Categoria.ProblemaInfraestructura, sugerida: "RiesgoElectrico");
        await using var _ = app;

        var detalle = await admin.DetalleAsync(id);

        Assert.Equal("ProblemaInfraestructura", detalle.Categoria);
        Assert.Equal("RiesgoElectrico", detalle.CategoriaSugerida);
    }

    [Fact]
    [Trait("AC", "AC-39")]
    public async Task El_empleado_creador_no_recibe_el_borrador()
    {
        await using var app = new ApiFactory();
        var empleado = await app.ClienteAutenticadoAsync();
        var creado = await empleado.CrearReporteAsync();
        await app.ProcesarClasificacionesAsync();

        var json = await empleado.DetalleJsonAsync(creado.Id);

        Assert.False(json.TryGetProperty("borradorMail", out _));
        Assert.False(json.TryGetProperty("accionSugerida", out _));
    }
}
