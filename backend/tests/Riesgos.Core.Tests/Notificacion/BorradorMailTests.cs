using Riesgos.Core.Dominio;
using Riesgos.Core.Notificacion;

namespace Riesgos.Core.Tests.Notificacion;

public class BorradorMailTests
{
    private static Reporte Reporte(Categoria categoria = Categoria.RiesgoElectrico, Criticidad criticidad = Criticidad.Alta) => new()
    {
        Id = 42,
        Descripcion = "Cable pelado junto al tablero",
        Ubicacion = "Planta 1, sector B",
        Categoria = categoria,
        Criticidad = criticidad,
        Tipo = TipoReporte.CondicionInsegura,
        FechaCreacion = new DateTimeOffset(2026, 10, 21, 12, 30, 0, TimeSpan.Zero),
    };

    [Fact]
    [Trait("AC", "AC-08")]
    public void Critica_sugiere_detener_actividad() =>
        Assert.Equal("Detener actividad", AccionSugerida.Para(Criticidad.Critica));

    [Fact]
    [Trait("AC", "AC-09")]
    public void Alta_sugiere_intervencion_inmediata() =>
        Assert.Equal("Requiere intervención inmediata", AccionSugerida.Para(Criticidad.Alta));

    [Theory]
    [Trait("AC", "AC-30")]
    [InlineData(Criticidad.Baja)]
    [InlineData(Criticidad.Media)]
    [InlineData(Criticidad.Pendiente)]
    public void Baja_media_o_pendiente_no_sugieren_accion(Criticidad criticidad) =>
        Assert.Null(AccionSugerida.Para(criticidad));

    [Fact]
    [Trait("AC", "AC-41")]
    public void El_asunto_contiene_categoria_y_criticidad()
    {
        var borrador = GeneradorBorrador.Generar(Reporte(), "ana@riesgos.test");

        Assert.Contains("Riesgo eléctrico", borrador.Asunto);
        Assert.Contains("Alta", borrador.Asunto);
        Assert.Contains("#42", borrador.Asunto);
    }

    [Fact]
    [Trait("AC", "AC-41")]
    public void El_cuerpo_contiene_descripcion_ubicacion_fecha_y_creador()
    {
        var borrador = GeneradorBorrador.Generar(Reporte(), "ana@riesgos.test");

        Assert.Contains("Cable pelado junto al tablero", borrador.Cuerpo);
        Assert.Contains("Planta 1, sector B", borrador.Cuerpo);
        Assert.Contains("21/10/2026 12:30", borrador.Cuerpo);
        Assert.Contains("ana@riesgos.test", borrador.Cuerpo);
    }

    [Fact]
    [Trait("AC", "AC-62")]
    [Trait("FR", "FR-022")]
    public void Con_clasificacion_pendiente_el_asunto_lo_indica()
    {
        var borrador = GeneradorBorrador.Generar(Reporte(Categoria.Pendiente, Criticidad.Pendiente), "ana@riesgos.test");

        Assert.Contains("Pendiente de clasificación", borrador.Asunto);
        Assert.DoesNotContain("Alta", borrador.Asunto);
    }
}
