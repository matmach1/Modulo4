using Microsoft.EntityFrameworkCore;
using Riesgos.Core.Dominio;
using Riesgos.Core.Imagenes;
using Riesgos.Core.Persistencia;
using Riesgos.Core.Validacion;

namespace Riesgos.Core.Servicios;

public sealed record ConfiguracionImagenes(string Ruta);

public sealed record ArchivoImagen(string Ruta, string TipoContenido);

// Imágenes guardadas en disco fuera de la carpeta pública; solo se sirven por el endpoint
// autenticado con las mismas reglas de acceso que el reporte (FR-013a).
public sealed class ServicioImagenes(RiesgosDbContext db, TimeProvider tiempo, ConfiguracionImagenes config)
{
    public async Task<ImagenAdjunta> AdjuntarAsync(int reporteId, UsuarioActual usuario, string? nombreArchivo, byte[] contenido, CancellationToken ct = default)
    {
        var reporte = await db.Reportes.SingleOrDefaultAsync(r => r.Id == reporteId, ct) ?? throw new NoEncontrado("El reporte no existe.");
        if (reporte.CreadorId != usuario.Id)
        {
            throw new AccesoDenegado("Solo quien creó el reporte puede adjuntar imágenes.");
        }

        if (reporte.Estado != EstadoReporte.Abierto)
        {
            throw new Conflicto("Solo se pueden adjuntar imágenes a un reporte en estado Abierto.");
        }

        var tipo = ValidadorImagen.Validar(nombreArchivo, contenido);
        if (await db.ImagenesAdjuntas.CountAsync(i => i.ReporteId == reporteId, ct) >= ImagenAdjunta.MaximoPorReporte)
        {
            throw new ErrorValidacion("archivo", $"Un reporte admite como máximo {ImagenAdjunta.MaximoPorReporte} imágenes.");
        }

        var imagen = new ImagenAdjunta
        {
            ReporteId = reporteId,
            NombreArchivo = $"{Guid.NewGuid():N}{tipo.Extension}",
            TipoContenido = tipo.TipoContenido,
            TamanoBytes = contenido.Length,
            FechaCarga = tiempo.GetUtcNow(),
        };
        db.ImagenesAdjuntas.Add(imagen);

        // Con el token de concurrencia del reporte, dos subidas simultáneas que leyeron la misma
        // cantidad no pueden pasar ambas: la segunda falla y el reporte nunca supera 5 (research R6).
        db.Entry(reporte).Property(r => r.RowVersion).IsModified = true;

        Directory.CreateDirectory(config.Ruta);
        var ruta = RutaDe(imagen);
        await File.WriteAllBytesAsync(ruta, contenido, ct);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            File.Delete(ruta);
            throw;
        }

        return imagen;
    }

    public async Task<ArchivoImagen> ObtenerAsync(int reporteId, int imagenId, UsuarioActual usuario, CancellationToken ct = default)
    {
        var reporte = await db.Reportes.AsNoTracking().SingleOrDefaultAsync(r => r.Id == reporteId, ct) ?? throw new NoEncontrado("El reporte no existe.");
        ServicioReportes.VerificarAcceso(reporte, usuario);
        var imagen = await db.ImagenesAdjuntas.AsNoTracking().SingleOrDefaultAsync(i => i.Id == imagenId && i.ReporteId == reporteId, ct)
            ?? throw new NoEncontrado("La imagen no existe.");
        return new ArchivoImagen(RutaDe(imagen), imagen.TipoContenido);
    }

    private string RutaDe(ImagenAdjunta imagen) => Path.Combine(Path.GetFullPath(config.Ruta), imagen.NombreArchivo);
}
