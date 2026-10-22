using System.Net;
using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US1;

public class MisReportesTests
{
    private static async Task<PaginaDto<ReporteDto>> MisReportesAsync(HttpClient cliente, string consulta = "") =>
        (await cliente.GetFromJsonAsync<PaginaDto<ReporteDto>>($"/api/reportes/mios{consulta}"))!;

    [Fact]
    [Trait("AC", "AC-29")]
    public async Task Cada_empleado_recibe_unicamente_sus_reportes()
    {
        await using var app = new ApiFactory();
        var a = await app.ClienteAutenticadoAsync();
        var b = await app.ClienteAutenticadoAsync();
        await a.CrearReporteAsync("Reporte de A 1");
        await a.CrearReporteAsync("Reporte de A 2");
        await b.CrearReporteAsync("Reporte de B");

        var deA = await MisReportesAsync(a);

        Assert.Equal(2, deA.Total);
        Assert.All(deA.Items, r => Assert.StartsWith("Reporte de A", r.Descripcion));
    }

    [Fact]
    [Trait("AC", "AC-12")]
    public async Task Sin_sesion_responde_401_y_no_muestra_datos()
    {
        await using var app = new ApiFactory();

        var respuesta = await app.CrearCliente().GetAsync("/api/reportes/mios");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.DoesNotContain("items", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("FR", "FR-011")]
    public async Task Ordena_del_mas_reciente_al_mas_antiguo_con_su_estado()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        await cliente.CrearReporteAsync("Primero");
        app.Reloj.Advance(TimeSpan.FromMinutes(1));
        await cliente.CrearReporteAsync("Segundo");

        var pagina = await MisReportesAsync(cliente);

        Assert.Equal(["Segundo", "Primero"], pagina.Items.Select(r => r.Descripcion));
        Assert.All(pagina.Items, r => Assert.Equal("Abierto", r.Estado));
    }

    [Fact]
    [Trait("FR", "FR-011")]
    public async Task Pagina_segun_page_y_size_y_rechaza_size_fuera_de_rango()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        for (var i = 0; i < 3; i++)
        {
            await cliente.CrearReporteAsync($"Reporte {i}");
            app.Reloj.Advance(TimeSpan.FromMinutes(1));
        }

        var segunda = await MisReportesAsync(cliente, "?page=2&size=2");
        var invalido = await cliente.GetAsync("/api/reportes/mios?size=101");

        Assert.Equal(3, segunda.Total);
        Assert.Equal(["Reporte 0"], segunda.Items.Select(r => r.Descripcion));
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-011")]
    public async Task El_administrador_tambien_tiene_su_lista_de_reportes_propios()
    {
        await using var app = new ApiFactory();
        var admin = await app.ComoAdminAsync();
        var empleado = await app.ClienteAutenticadoAsync();
        await admin.CrearReporteAsync("Del admin");
        await empleado.CrearReporteAsync("Del empleado");

        var propios = await MisReportesAsync(admin);

        Assert.Equal(["Del admin"], propios.Items.Select(r => r.Descripcion));
    }
}
