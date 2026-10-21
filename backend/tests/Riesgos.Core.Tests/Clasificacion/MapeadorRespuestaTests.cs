using Riesgos.Clasificacion;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Tests.Clasificacion;

public class MapeadorRespuestaTests
{
    private const string FinNormal = "end_turn";

    private static string Json(string tipo, string categoria, string criticidad) =>
        $$"""{"tipo":"{{tipo}}","categoria":"{{categoria}}","criticidad":"{{criticidad}}"}""";

    [Theory]
    [Trait("FR", "FR-014")]
    [InlineData("Acto inseguro", "Problema de infraestructura", "Baja", TipoReporte.ActoInseguro, Categoria.ProblemaInfraestructura, Criticidad.Baja)]
    [InlineData("Condición insegura", "Riesgo eléctrico", "Media", TipoReporte.CondicionInsegura, Categoria.RiesgoElectrico, Criticidad.Media)]
    [InlineData("Casi accidente", "Uso de elementos de protección personal", "Alta", TipoReporte.CasiAccidente, Categoria.UsoEpp, Criticidad.Alta)]
    [InlineData("Riesgo operativo", "Otro", "Crítica", TipoReporte.RiesgoOperativo, Categoria.Otro, Criticidad.Critica)]
    [InlineData("Otro", "Otro", "Baja", TipoReporte.Otro, Categoria.Otro, Criticidad.Baja)]
    public void Respuesta_valida_se_mapea_a_los_enums(string tipo, string categoria, string criticidad, TipoReporte t, Categoria c, Criticidad k)
    {
        var resultado = MapeadorRespuesta.Mapear(Json(tipo, categoria, criticidad), FinNormal);

        Assert.Equal(new ResultadoClasificacion.Exito(t, c, k), resultado);
    }

    [Theory]
    [Trait("FR", "FR-018")]
    [InlineData("Acto inseguro", "Problema de infraestructura", "Urgente")] // fuera de lista
    [InlineData("acto inseguro", "Problema de infraestructura", "Baja")]    // otra capitalización
    [InlineData("Condicion insegura", "Riesgo eléctrico", "Media")]         // sin tilde
    [InlineData("Acto inseguro", "Pendiente de clasificación", "Baja")]     // pendiente no es un valor del modelo
    [InlineData("Acto inseguro", "Riesgo electrico ", "Baja")]              // espacio extra
    public void Valor_que_no_coincide_exactamente_es_respuesta_invalida(string tipo, string categoria, string criticidad)
    {
        var resultado = MapeadorRespuesta.Mapear(Json(tipo, categoria, criticidad), FinNormal);

        Assert.Equal(new ResultadoClasificacion.Falla(MotivoFalla.RespuestaInvalida), resultado);
    }

    [Theory]
    [Trait("FR", "FR-018")]
    [InlineData("""{"tipo":"Otro","categoria":"Otro"}""")]
    [InlineData("no es json")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("""{"tipo":1,"categoria":"Otro","criticidad":"Baja"}""")]
    public void Json_incompleto_o_invalido_es_respuesta_invalida(string? json)
    {
        Assert.Equal(new ResultadoClasificacion.Falla(MotivoFalla.RespuestaInvalida), MapeadorRespuesta.Mapear(json, FinNormal));
    }

    [Theory]
    [Trait("FR", "FR-018")]
    [InlineData("max_tokens")]
    [InlineData("refusal")]
    [InlineData(null)]
    public void Fin_distinto_de_end_turn_es_respuesta_invalida(string? stopReason)
    {
        var resultado = MapeadorRespuesta.Mapear(Json("Otro", "Otro", "Baja"), stopReason);

        Assert.Equal(new ResultadoClasificacion.Falla(MotivoFalla.RespuestaInvalida), resultado);
    }
}
