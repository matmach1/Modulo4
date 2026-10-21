using System.Net;
using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Api.Tests.US2;

public class ClasificacionTests
{
    private static readonly string[] TiposValidos = ["ActoInseguro", "CondicionInsegura", "CasiAccidente", "RiesgoOperativo", "Otro"];
    private static readonly string[] CategoriasValidas = ["ProblemaInfraestructura", "RiesgoElectrico", "UsoEpp", "Otro"];
    private static readonly string[] CriticidadesValidas = ["Baja", "Media", "Alta", "Critica"];

    [Fact]
    [Trait("AC", "AC-40")]
    public async Task Tras_clasificar_tipo_categoria_y_criticidad_son_valores_validos()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        var creado = await cliente.CrearReporteAsync();

        await app.ProcesarClasificacionesAsync();

        var reporte = await cliente.DetalleAsync(creado.Id);
        Assert.Contains(reporte.Tipo, TiposValidos);
        Assert.Contains(reporte.Categoria, CategoriasValidas);
        Assert.Contains(reporte.Criticidad, CriticidadesValidas);
    }

    [Fact]
    [Trait("AC", "AC-06")]
    [Trait("FR", "FR-016")]
    public async Task Con_categoria_sugerida_distinta_la_definitiva_es_la_automatica_y_se_guardan_ambas()
    {
        await using var app = new ApiFactory();
        app.Clasificador.Resultado = new ResultadoClasificacion.Exito(TipoReporte.CondicionInsegura, Categoria.ProblemaInfraestructura, Criticidad.Media);
        var cliente = await app.ClienteAutenticadoAsync();
        var creado = await cliente.CrearReporteAsync(categoriaSugerida: "RiesgoElectrico");

        await app.ProcesarClasificacionesAsync();

        var reporte = await cliente.DetalleAsync(creado.Id);
        Assert.Equal("ProblemaInfraestructura", reporte.Categoria);
        Assert.Equal("RiesgoElectrico", reporte.CategoriaSugerida);
    }

    [Fact]
    [Trait("AC", "AC-56")]
    public async Task Si_el_clasificador_devuelve_error_el_reporte_se_crea_y_queda_pendiente()
    {
        await using var app = new ApiFactory();
        app.Clasificador.Resultado = new ResultadoClasificacion.Falla(MotivoFalla.ErrorServicio);
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearAsync();
        await app.ProcesarClasificacionesAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var reporte = (await cliente.MisReportesAsync()).Items.Single();
        Assert.Equal("Pendiente", reporte.Tipo);
        Assert.Equal("Pendiente", reporte.Categoria);
        Assert.Equal("Pendiente", reporte.Criticidad);
        Assert.Single(app.Clasificador.Entradas);
    }

    [Fact]
    [Trait("AC", "AC-58")]
    public async Task Si_el_clasificador_no_responde_en_10_segundos_el_reporte_queda_pendiente()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearAsync();
        await app.ProcesarSinRespuestaAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var reporte = (await cliente.MisReportesAsync()).Items.Single();
        Assert.Equal("Pendiente", reporte.Criticidad);
    }

    [Fact]
    [Trait("AC", "AC-23")]
    [Trait("FR", "FR-015")]
    public async Task Al_clasificar_un_reporte_con_imagenes_la_solicitud_no_contiene_imagenes()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        var creado = await cliente.CrearReporteAsync();
        await app.ProcesarClasificacionesAsync();
        (await cliente.SubirImagenAsync(creado.Id)).EnsureSuccessStatusCode();

        await cliente.PutAsJsonAsync($"/api/reportes/{creado.Id}", new { descripcion = "Con foto adjunta", ubicacion = "Planta 1" });
        await app.ProcesarClasificacionesAsync();

        // La entrada al clasificador solo tiene campos de texto; nada de la imagen llega al modelo.
        Assert.Equal(new EntradaClasificacion("Con foto adjunta", "Planta 1", null), app.Clasificador.Entradas.Last());
        Assert.All(typeof(EntradaClasificacion).GetProperties(),
            p => Assert.True(p.PropertyType == typeof(string) || p.PropertyType == typeof(Categoria?), $"{p.Name} no es texto"));
    }
}
