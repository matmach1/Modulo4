using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US5;

public class ImagenesTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private const int DosMB = 2 * 1024 * 1024;

    private static async Task<HttpResponseMessage> SubirAsync(HttpClient cliente, int id, byte[] contenido, string nombre = "foto.jpg", int archivos = 1)
    {
        using var formulario = new MultipartFormDataContent();
        for (var i = 0; i < archivos; i++)
        {
            var parte = new ByteArrayContent(contenido);
            parte.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            formulario.Add(parte, "archivo", nombre);
        }

        return await cliente.PostAsync($"/api/reportes/{id}/imagenes", formulario);
    }

    private static async Task<List<JsonElement>> ImagenesAsync(HttpClient cliente, int id) =>
        (await cliente.DetalleJsonAsync(id)).GetProperty("imagenes").EnumerateArray().ToList();

    private static async Task<(ApiFactory App, HttpClient Empleado, int Id)> PrepararAsync()
    {
        var app = new ApiFactory();
        var empleado = await app.ClienteAutenticadoAsync();
        var creado = await empleado.CrearReporteAsync();
        return (app, empleado, creado.Id);
    }

    [Fact]
    [Trait("AC", "AC-22")]
    public async Task La_imagen_adjunta_aparece_en_el_detalle_del_reporte()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await SubirAsync(empleado, id, Jpeg);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var imagen = Assert.Single(await ImagenesAsync(empleado, id));
        Assert.Equal("image/jpeg", imagen.GetProperty("tipoContenido").GetString());
        Assert.StartsWith($"/api/reportes/{id}/imagenes/", imagen.GetProperty("url").GetString());
    }

    [Fact]
    [Trait("AC", "AC-19")]
    public async Task Con_5_imagenes_una_sexta_responde_400_y_siguen_5()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;
        for (var i = 0; i < 5; i++)
        {
            (await SubirAsync(empleado, id, Jpeg)).EnsureSuccessStatusCode();
        }

        var sexta = await SubirAsync(empleado, id, Jpeg);

        Assert.Equal(HttpStatusCode.BadRequest, sexta.StatusCode);
        Assert.Equal(5, (await ImagenesAsync(empleado, id)).Count);
    }

    [Fact]
    [Trait("AC", "AC-20")]
    public async Task Formato_distinto_de_jpg_jpeg_o_png_responde_400_y_no_se_asocia()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;

        var texto = await SubirAsync(empleado, id, "no soy una imagen"u8.ToArray(), "notas.txt");
        var renombrado = await SubirAsync(empleado, id, "no soy una imagen"u8.ToArray(), "foto.jpg");

        Assert.Equal(HttpStatusCode.BadRequest, texto.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, renombrado.StatusCode);
        Assert.Empty(await ImagenesAsync(empleado, id));
    }

    [Fact]
    [Trait("AC", "AC-21")]
    public async Task Archivo_de_mas_de_2_MB_responde_400_y_no_se_asocia()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;
        var grande = new byte[DosMB + 1];
        Jpeg.CopyTo(grande, 0);

        var respuesta = await SubirAsync(empleado, id, grande);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await ImagenesAsync(empleado, id));
    }

    [Fact]
    [Trait("FR", "FR-013")]
    public async Task Mas_de_un_archivo_en_la_misma_solicitud_responde_400()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;

        var respuesta = await SubirAsync(empleado, id, Jpeg, archivos: 2);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Empty(await ImagenesAsync(empleado, id));
    }

    [Theory]
    [Trait("AC", "AC-53")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task En_EnProgreso_o_Cerrado_responde_409_y_la_cantidad_no_cambia(bool cerrar)
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;
        var admin = await app.ComoAdminAsync();
        await admin.PostAsJsonAsync($"/api/admin/reportes/{id}/estado", new { estado = "EnProgreso" });
        if (cerrar)
        {
            await admin.PostAsJsonAsync($"/api/admin/reportes/{id}/estado", new { estado = "Cerrado", accionRealizada = "Se reparó", observacionCierre = "Ok" });
        }

        var respuesta = await SubirAsync(empleado, id, Jpeg);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Empty(await ImagenesAsync(empleado, id));
    }

    [Fact]
    [Trait("FR", "FR-013")]
    public async Task Dos_subidas_simultaneas_con_4_cargadas_dejan_el_reporte_con_5()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;
        for (var i = 0; i < 4; i++)
        {
            (await SubirAsync(empleado, id, Jpeg)).EnsureSuccessStatusCode();
        }

        var respuestas = await Task.WhenAll(SubirAsync(empleado, id, Jpeg), SubirAsync(empleado, id, Jpeg));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(5, (await ImagenesAsync(empleado, id)).Count);
    }

    [Fact]
    [Trait("AC", "AC-36")]
    public async Task Otro_empleado_no_puede_adjuntar_a_un_reporte_ajeno()
    {
        var (app, _, id) = await PrepararAsync();
        await using var _ = app;
        var otro = await app.ClienteAutenticadoAsync();

        var respuesta = await SubirAsync(otro, id, Jpeg);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-73")]
    [Trait("FR", "FR-013a")]
    public async Task Solo_el_creador_y_el_administrador_pueden_descargar_la_imagen()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;
        await SubirAsync(empleado, id, Jpeg);
        var url = (await ImagenesAsync(empleado, id)).Single().GetProperty("url").GetString()!;
        var admin = await app.ComoAdminAsync();
        var otro = await app.ClienteAutenticadoAsync();

        var delCreador = await empleado.GetAsync(url);
        var delAdmin = await admin.GetAsync(url);
        var deOtro = await otro.GetAsync(url);
        var anonimo = await app.CrearCliente().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, delCreador.StatusCode);
        Assert.Equal("image/jpeg", delCreador.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Jpeg, await delCreador.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.OK, delAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deOtro.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-013a")]
    public async Task Una_imagen_pedida_bajo_otro_reporte_responde_404()
    {
        var (app, empleado, id) = await PrepararAsync();
        await using var _ = app;
        var otroReporte = await empleado.CrearReporteAsync("Otro reporte");
        await SubirAsync(empleado, id, Jpeg);
        var imagenId = (await ImagenesAsync(empleado, id)).Single().GetProperty("id").GetInt32();

        var respuesta = await empleado.GetAsync($"/api/reportes/{otroReporte.Id}/imagenes/{imagenId}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
