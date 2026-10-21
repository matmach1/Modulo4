using System.Text.Json;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Clasificacion;

// Traduce la respuesta del modelo a los enums por igualdad exacta con los textos del JSON Schema
// (contracts/clasificador.md). Cualquier otra cosa es respuesta inválida (FR-018).
public static class MapeadorRespuesta
{
    public static readonly IReadOnlyDictionary<string, TipoReporte> Tipos = new Dictionary<string, TipoReporte>(StringComparer.Ordinal)
    {
        ["Acto inseguro"] = TipoReporte.ActoInseguro,
        ["Condición insegura"] = TipoReporte.CondicionInsegura,
        ["Casi accidente"] = TipoReporte.CasiAccidente,
        ["Riesgo operativo"] = TipoReporte.RiesgoOperativo,
        ["Otro"] = TipoReporte.Otro,
    };

    public static readonly IReadOnlyDictionary<string, Categoria> Categorias = new Dictionary<string, Categoria>(StringComparer.Ordinal)
    {
        ["Problema de infraestructura"] = Categoria.ProblemaInfraestructura,
        ["Riesgo eléctrico"] = Categoria.RiesgoElectrico,
        ["Uso de elementos de protección personal"] = Categoria.UsoEpp,
        ["Otro"] = Categoria.Otro,
    };

    public static readonly IReadOnlyDictionary<string, Criticidad> Criticidades = new Dictionary<string, Criticidad>(StringComparer.Ordinal)
    {
        ["Baja"] = Criticidad.Baja,
        ["Media"] = Criticidad.Media,
        ["Alta"] = Criticidad.Alta,
        ["Crítica"] = Criticidad.Critica,
    };

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
}
