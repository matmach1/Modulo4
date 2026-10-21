using System.Text.Json;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Clasificacion;

// Traduce la respuesta del modelo a los enums por igualdad exacta con los textos del JSON Schema
// (contracts/clasificador.md). Cualquier otra cosa es respuesta inválida (FR-018).
public static class MapeadorRespuesta
{
    // Valores permitidos al modelo: los textos de la spec, sin "Pendiente de clasificación".
    public static readonly IReadOnlyDictionary<string, TipoReporte> Tipos = Invertir(Textos.Tipos, TipoReporte.Pendiente);

    public static readonly IReadOnlyDictionary<string, Categoria> Categorias = Invertir(Textos.Categorias, Categoria.Pendiente);

    public static readonly IReadOnlyDictionary<string, Criticidad> Criticidades = Invertir(Textos.Criticidades, Criticidad.Pendiente);

    private static readonly ResultadoClasificacion Invalida = new ResultadoClasificacion.Falla(MotivoFalla.RespuestaInvalida);

    public static ResultadoClasificacion Mapear(string? json, string? stopReason)
    {
        if (stopReason != "end_turn" || string.IsNullOrWhiteSpace(json))
        {
            return Invalida;
        }

        try
        {
            using var documento = JsonDocument.Parse(json);
            var raiz = documento.RootElement;
            if (raiz.ValueKind == JsonValueKind.Object
                && Leer(raiz, "tipo", Tipos, out var tipo)
                && Leer(raiz, "categoria", Categorias, out var categoria)
                && Leer(raiz, "criticidad", Criticidades, out var criticidad))
            {
                return new ResultadoClasificacion.Exito(tipo, categoria, criticidad);
            }

            return Invalida;
        }
        catch (JsonException)
        {
            return Invalida;
        }
    }

    private static bool Leer<T>(JsonElement raiz, string propiedad, IReadOnlyDictionary<string, T> valores, out T valor)
    {
        valor = default!;
        return raiz.TryGetProperty(propiedad, out var elemento)
            && elemento.ValueKind == JsonValueKind.String
            && valores.TryGetValue(elemento.GetString()!, out valor!);
    }

    private static Dictionary<string, T> Invertir<T>(IReadOnlyDictionary<T, string> textos, T pendiente)
        where T : struct, Enum =>
        textos.Where(par => !par.Key.Equals(pendiente)).ToDictionary(par => par.Value, par => par.Key, StringComparer.Ordinal);
}
