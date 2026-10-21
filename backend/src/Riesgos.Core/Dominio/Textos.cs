namespace Riesgos.Core.Dominio;

// Textos de la spec para cada valor de clasificación: los usan el borrador de mail y el esquema
// que se le pide al modelo, para que ambos hablen exactamente igual.
public static class Textos
{
    public const string Pendiente = "Pendiente de clasificación";

    public static readonly IReadOnlyDictionary<TipoReporte, string> Tipos = new Dictionary<TipoReporte, string>
    {
        [TipoReporte.Pendiente] = Pendiente,
        [TipoReporte.ActoInseguro] = "Acto inseguro",
        [TipoReporte.CondicionInsegura] = "Condición insegura",
        [TipoReporte.CasiAccidente] = "Casi accidente",
        [TipoReporte.RiesgoOperativo] = "Riesgo operativo",
        [TipoReporte.Otro] = "Otro",
    };

    public static readonly IReadOnlyDictionary<Categoria, string> Categorias = new Dictionary<Categoria, string>
    {
        [Categoria.Pendiente] = Pendiente,
        [Categoria.ProblemaInfraestructura] = "Problema de infraestructura",
        [Categoria.RiesgoElectrico] = "Riesgo eléctrico",
        [Categoria.UsoEpp] = "Uso de elementos de protección personal",
        [Categoria.Otro] = "Otro",
    };

    public static readonly IReadOnlyDictionary<Criticidad, string> Criticidades = new Dictionary<Criticidad, string>
    {
        [Criticidad.Pendiente] = Pendiente,
        [Criticidad.Baja] = "Baja",
        [Criticidad.Media] = "Media",
        [Criticidad.Alta] = "Alta",
        [Criticidad.Critica] = "Crítica",
    };
}
