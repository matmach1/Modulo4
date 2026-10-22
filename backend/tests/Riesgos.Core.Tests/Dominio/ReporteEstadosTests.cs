using Riesgos.Core.Dominio;
using Riesgos.Core.Validacion;

namespace Riesgos.Core.Tests.Dominio;

// Tabla de transiciones de data-model.md (FR-027, FR-029).
public class ReporteEstadosTests
{
    private const int AdminId = 7;
    private static readonly DateTimeOffset Ahora = new(2026, 10, 21, 15, 0, 0, TimeSpan.Zero);

    private static Reporte EnEstado(EstadoReporte estado) => new() { Descripcion = "d", Ubicacion = "u", Estado = estado };

    [Fact]
    [Trait("AC", "AC-15")]
    public void De_Abierto_a_EnProgreso_registra_fecha_y_administrador()
    {
        var reporte = EnEstado(EstadoReporte.Abierto);

        reporte.CambiarEstado(EstadoReporte.EnProgreso, AdminId, Ahora);

        Assert.Equal(EstadoReporte.EnProgreso, reporte.Estado);
        Assert.Equal(Ahora, reporte.FechaInicioProgreso);
        Assert.Equal(AdminId, reporte.UsuarioInicioProgresoId);
    }

    [Fact]
    [Trait("AC", "AC-35")]
    [Trait("AC", "AC-18")]
    public void De_EnProgreso_a_Cerrado_con_ambos_textos_registra_el_cierre()
    {
        var reporte = EnEstado(EstadoReporte.EnProgreso);

        reporte.CambiarEstado(EstadoReporte.Cerrado, AdminId, Ahora, "  Se reemplazó el cable ", "Sin novedades");

        Assert.Equal(EstadoReporte.Cerrado, reporte.Estado);
        Assert.Equal("Se reemplazó el cable", reporte.AccionRealizada);
        Assert.Equal("Sin novedades", reporte.ObservacionCierre);
        Assert.Equal(Ahora, reporte.FechaCierre);
        Assert.Equal(AdminId, reporte.UsuarioCierreId);
    }

    [Theory]
    [Trait("AC", "AC-34")]
    [Trait("AC", "AC-33")]
    [InlineData(EstadoReporte.Abierto, EstadoReporte.Cerrado)]
    [InlineData(EstadoReporte.EnProgreso, EstadoReporte.Abierto)]
    [InlineData(EstadoReporte.Cerrado, EstadoReporte.Abierto)]
    [InlineData(EstadoReporte.Cerrado, EstadoReporte.EnProgreso)]
    [InlineData(EstadoReporte.Abierto, EstadoReporte.Abierto)]
    [InlineData(EstadoReporte.EnProgreso, EstadoReporte.EnProgreso)]
    [InlineData(EstadoReporte.Cerrado, EstadoReporte.Cerrado)]
    public void Cualquier_otra_transicion_es_conflicto_y_no_cambia_el_estado(EstadoReporte desde, EstadoReporte hacia)
    {
        var reporte = EnEstado(desde);

        Assert.Throws<Conflicto>(() => reporte.CambiarEstado(hacia, AdminId, Ahora, "acción", "observación"));
        Assert.Equal(desde, reporte.Estado);
    }

    [Theory]
    [Trait("AC", "AC-25")]
    [InlineData(null, "observación")]
    [InlineData("acción", null)]
    [InlineData("   ", "observación")]
    [InlineData("acción", "  ")]
    public void Cerrar_sin_accion_realizada_u_observacion_es_dato_invalido(string? accion, string? observacion)
    {
        var reporte = EnEstado(EstadoReporte.EnProgreso);

        Assert.Throws<ErrorValidacion>(() => reporte.CambiarEstado(EstadoReporte.Cerrado, AdminId, Ahora, accion, observacion));
        Assert.Equal(EstadoReporte.EnProgreso, reporte.Estado);
    }

    [Fact]
    [Trait("FR", "FR-029")]
    public void Textos_de_cierre_de_mas_de_1000_caracteres_son_dato_invalido()
    {
        var reporte = EnEstado(EstadoReporte.EnProgreso);

        Assert.Throws<ErrorValidacion>(() =>
            reporte.CambiarEstado(EstadoReporte.Cerrado, AdminId, Ahora, new string('a', 1001), "observación"));
    }
}
