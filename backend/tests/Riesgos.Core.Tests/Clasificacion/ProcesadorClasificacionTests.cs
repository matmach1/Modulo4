using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Tests.Clasificacion;

public sealed class ProcesadorClasificacionTests : IDisposable
{
    private static readonly ResultadoClasificacion.Exito Valida =
        new(TipoReporte.CasiAccidente, Categoria.RiesgoElectrico, Criticidad.Critica);

    private readonly BaseDePrueba _base = new();
    private readonly FakeTimeProvider _reloj = new(DateTimeOffset.UnixEpoch);
    private readonly ClasificadorFalso _clasificador = new();

    public void Dispose() => _base.Dispose();

    private async Task ProcesarAsync(TrabajoClasificacion trabajo)
    {
        await using var db = _base.NuevoContexto();
        var procesador = new ProcesadorClasificacion(db, _clasificador, _reloj, NullLogger<ProcesadorClasificacion>.Instance);
        await procesador.ProcesarAsync(trabajo, CancellationToken.None);
    }

    [Fact]
    [Trait("FR", "FR-014")]
    public async Task Exito_guarda_tipo_categoria_y_criticidad()
    {
        var reporte = await _base.CrearReporteAsync();
        _clasificador.Responder = _ => Task.FromResult<ResultadoClasificacion>(Valida);

        await ProcesarAsync(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));

        var guardado = await _base.LeerAsync(reporte.Id);
        Assert.Equal(TipoReporte.CasiAccidente, guardado.Tipo);
        Assert.Equal(Categoria.RiesgoElectrico, guardado.Categoria);
        Assert.Equal(Criticidad.Critica, guardado.Criticidad);
    }

    [Fact]
    [Trait("FR", "FR-018")]
    public async Task Falla_deja_el_reporte_pendiente()
    {
        var reporte = await _base.CrearReporteAsync();
        _clasificador.Responder = _ => Task.FromResult<ResultadoClasificacion>(new ResultadoClasificacion.Falla(MotivoFalla.ErrorServicio));

        await ProcesarAsync(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));

        var guardado = await _base.LeerAsync(reporte.Id);
        Assert.Equal(TipoReporte.Pendiente, guardado.Tipo);
        Assert.Equal(Categoria.Pendiente, guardado.Categoria);
        Assert.Equal(Criticidad.Pendiente, guardado.Criticidad);
    }

    [Fact]
    [Trait("FR", "FR-018")]
    public async Task Una_excepcion_del_clasificador_deja_el_reporte_pendiente_sin_propagarse()
    {
        var reporte = await _base.CrearReporteAsync();
        _clasificador.Responder = _ => throw new HttpRequestException("caído");

        await ProcesarAsync(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));

        Assert.Equal(Criticidad.Pendiente, (await _base.LeerAsync(reporte.Id)).Criticidad);
    }

    [Fact]
    [Trait("AC", "AC-58")]
    public async Task Sin_respuesta_en_10_segundos_queda_pendiente()
    {
        var reporte = await _base.CrearReporteAsync();
        var llamado = new TaskCompletionSource();
        _clasificador.Responder = async ct =>
        {
            llamado.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return Valida;
        };

        var procesando = ProcesarAsync(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));
        await Task.WhenAny(llamado.Task, procesando).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(llamado.Task.IsCompleted, "El procesador no llamó al clasificador.");
        _reloj.Advance(TimeSpan.FromSeconds(9));
        Assert.False(procesando.IsCompleted);
        _reloj.Advance(TimeSpan.FromSeconds(1));
        await procesando.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(Criticidad.Pendiente, (await _base.LeerAsync(reporte.Id)).Criticidad);
    }

    [Fact]
    [Trait("FR", "FR-017")]
    public async Task Un_trabajo_de_una_version_vieja_se_descarta_sin_llamar_al_clasificador()
    {
        var reporte = await _base.CrearReporteAsync();

        await ProcesarAsync(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion - 1));

        Assert.Equal(0, _clasificador.Llamadas);
    }

    [Fact]
    [Trait("FR", "FR-017")]
    public async Task Si_el_reporte_cambia_de_version_mientras_se_clasifica_el_resultado_no_se_guarda()
    {
        var reporte = await _base.CrearReporteAsync();
        _clasificador.Responder = async _ =>
        {
            // Otra edición del creador llega mientras la IA responde.
            await using var db = _base.NuevoContexto();
            await db.Reportes.Where(r => r.Id == reporte.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.VersionClasificacion, r => r.VersionClasificacion + 1));
            return Valida;
        };

        await ProcesarAsync(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));

        Assert.Equal(Criticidad.Pendiente, (await _base.LeerAsync(reporte.Id)).Criticidad);
    }

    [Fact]
    [Trait("AC", "AC-23")]
    [Trait("FR", "FR-015")]
    public async Task La_entrada_al_clasificador_solo_contiene_el_texto_del_reporte()
    {
        var reporte = await _base.CrearReporteAsync("Derrame de aceite", "Depósito", Categoria.Otro);
        _clasificador.Responder = _ => Task.FromResult<ResultadoClasificacion>(Valida);

        await ProcesarAsync(new TrabajoClasificacion(reporte.Id, reporte.VersionClasificacion));

        Assert.Equal(new EntradaClasificacion("Derrame de aceite", "Depósito", Categoria.Otro), Assert.Single(_clasificador.Entradas));
    }

    [Fact]
    public async Task Un_reporte_inexistente_no_produce_error()
    {
        await ProcesarAsync(new TrabajoClasificacion(9999, 1));

        Assert.Equal(0, _clasificador.Llamadas);
    }

    private sealed class ClasificadorFalso : IClasificadorReportes
    {
        public Func<CancellationToken, Task<ResultadoClasificacion>> Responder { get; set; } =
            _ => Task.FromResult<ResultadoClasificacion>(new ResultadoClasificacion.Falla(MotivoFalla.ErrorServicio));

        public List<EntradaClasificacion> Entradas { get; } = [];

        public int Llamadas => Entradas.Count;

        public Task<ResultadoClasificacion> ClasificarAsync(EntradaClasificacion entrada, CancellationToken ct)
        {
            Entradas.Add(entrada);
            return Responder(ct);
        }
    }
}
