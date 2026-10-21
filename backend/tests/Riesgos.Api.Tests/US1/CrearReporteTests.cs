using System.Net;
using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US1;

public class CrearReporteTests
{
    [Fact]
    [Trait("AC", "AC-27")]
    public async Task Reporte_valido_responde_201_y_queda_Abierto()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await respuesta.Content.ReadFromJsonAsync<ReporteDto>();
        var reporte = await cliente.DetalleAsync(creado!.Id);
        Assert.Equal("Abierto", reporte.Estado);
        Assert.Equal(Reportes.Descripcion, reporte.Descripcion);
        Assert.Equal(Reportes.Ubicacion, reporte.Ubicacion);
    }

    [Fact]
    [Trait("AC", "AC-75")]
    [Trait("FR", "FR-014")]
    public async Task La_creacion_responde_de_inmediato_con_clasificacion_pendiente_y_encola_el_trabajo()
    {
        await using var app = new ApiFactory();
        app.Clasificador.NoResponder = true;
        var cliente = await app.ClienteAutenticadoAsync();

        var reporte = await cliente.CrearReporteAsync();

        Assert.Equal("Pendiente", reporte.Tipo);
        Assert.Equal("Pendiente", reporte.Categoria);
        Assert.Equal("Pendiente", reporte.Criticidad);
        Assert.Equal(1, app.Cola.Cantidad);
        Assert.Empty(app.Clasificador.Entradas);
    }

    [Theory]
    [Trait("AC", "AC-01")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Descripcion_vacia_responde_400_y_no_crea(string descripcion)
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearAsync(descripcion: descripcion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await cliente.CantidadPropiosAsync());
    }

    [Fact]
    [Trait("AC", "AC-28")]
    public async Task Ubicacion_vacia_responde_400_y_no_crea()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearAsync(ubicacion: "");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await cliente.CantidadPropiosAsync());
    }

    [Fact]
    [Trait("AC", "AC-67")]
    [Trait("FR", "FR-019")]
    public async Task Descripcion_de_mas_de_2000_caracteres_responde_400_y_no_pide_clasificacion()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearAsync(descripcion: new string('a', 2001));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await cliente.CantidadPropiosAsync());
        Assert.Equal(0, app.Cola.Cantidad);
        Assert.Empty(app.Clasificador.Entradas);
    }

    [Fact]
    [Trait("AC", "AC-68")]
    public async Task Ubicacion_de_mas_de_200_caracteres_responde_400_y_no_crea()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearAsync(ubicacion: new string('b', 201));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await cliente.CantidadPropiosAsync());
    }

    [Fact]
    [Trait("FR", "FR-009")]
    public async Task Categoria_sugerida_invalida_responde_400()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var invalida = await cliente.CrearAsync(categoriaSugerida: "Inventada");
        var pendiente = await cliente.CrearAsync(categoriaSugerida: "Pendiente");

        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, pendiente.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-009")]
    public async Task Categoria_sugerida_valida_queda_guardada()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();

        var reporte = await cliente.CrearReporteAsync(categoriaSugerida: "RiesgoElectrico");

        Assert.Equal("RiesgoElectrico", reporte.CategoriaSugerida);
    }

    [Fact]
    [Trait("FR", "FR-009a")]
    public async Task El_administrador_tambien_puede_crear_reportes()
    {
        await using var app = new ApiFactory();
        var admin = await app.ComoAdminAsync();

        var respuesta = await admin.CrearAsync();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-12")]
    public async Task Sin_sesion_no_se_puede_crear()
    {
        await using var app = new ApiFactory();

        var respuesta = await app.CrearCliente().CrearAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
