using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Riesgos.Api.Tests.Infraestructura;

// Vista de los DTOs de la API tal como los ve un cliente (contracts/api.md).
public sealed record ReporteDto(
    int Id,
    string Descripcion,
    string Ubicacion,
    string Estado,
    string Tipo,
    string Categoria,
    string Criticidad,
    string? CategoriaSugerida,
    DateTimeOffset FechaCreacion,
    string CreadorEmail);

public sealed record PaginaDto<T>(List<T> Items, int Page, int Size, int Total);

public static class Reportes
{
    public const string Descripcion = "Cable pelado junto al tablero eléctrico";
    public const string Ubicacion = "Planta 1, sector B";

    public static Task<HttpResponseMessage> CrearAsync(this HttpClient cliente, string descripcion = Descripcion, string ubicacion = Ubicacion, string? categoriaSugerida = null) =>
        cliente.PostAsJsonAsync("/api/reportes", new { descripcion, ubicacion, categoriaSugerida });

    public static async Task<ReporteDto> CrearReporteAsync(this HttpClient cliente, string descripcion = Descripcion, string ubicacion = Ubicacion, string? categoriaSugerida = null)
    {
        var respuesta = await cliente.CrearAsync(descripcion, ubicacion, categoriaSugerida);
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<ReporteDto>())!;
    }

    public static async Task<ReporteDto> DetalleAsync(this HttpClient cliente, int id) =>
        (await cliente.GetFromJsonAsync<ReporteDto>($"/api/reportes/{id}"))!;

    public static async Task<PaginaDto<ReporteDto>> MisReportesAsync(this HttpClient cliente) =>
        (await cliente.GetFromJsonAsync<PaginaDto<ReporteDto>>("/api/reportes/mios"))!;

    public static async Task<int> CantidadPropiosAsync(this HttpClient cliente) =>
        (await cliente.MisReportesAsync()).Total;

    public static readonly byte[] JpegMinimo = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    public static async Task<HttpResponseMessage> SubirImagenAsync(this HttpClient cliente, int id)
    {
        using var formulario = new MultipartFormDataContent();
        var parte = new ByteArrayContent(JpegMinimo);
        parte.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        formulario.Add(parte, "archivo", "foto.jpg");
        return await cliente.PostAsync($"/api/reportes/{id}/imagenes", formulario);
    }

    // Detalle como JSON crudo, para verificar qué propiedades incluye o no la respuesta.
    public static async Task<JsonElement> DetalleJsonAsync(this HttpClient cliente, int id) =>
        await cliente.GetFromJsonAsync<JsonElement>($"/api/reportes/{id}");
}
