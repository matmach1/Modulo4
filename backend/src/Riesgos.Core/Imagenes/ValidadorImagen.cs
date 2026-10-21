using Riesgos.Core.Validacion;

namespace Riesgos.Core.Imagenes;

public sealed record TipoImagen(string TipoContenido, string Extension);

// Valida por extensión y por el contenido real del archivo (firma de bytes), para que un archivo
// cualquiera renombrado a .jpg no pase (research R6, AC-20, AC-21).
public static class ValidadorImagen
{
    public const int TamanoMaximo = 2 * 1024 * 1024;

    private static readonly byte[] FirmaJpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly TipoImagen Jpeg = new("image/jpeg", ".jpg");
    private static readonly TipoImagen Png = new("image/png", ".png");

    public static TipoImagen Validar(string? nombreArchivo, byte[] contenido)
    {
        if (contenido.Length == 0)
        {
            throw new ErrorValidacion("archivo", "El archivo está vacío.");
        }

        if (contenido.Length > TamanoMaximo)
        {
            throw new ErrorValidacion("archivo", "La imagen supera el máximo de 2 MB.");
        }

        var esperado = Path.GetExtension(nombreArchivo ?? string.Empty).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => Jpeg,
            ".png" => Png,
            _ => throw new ErrorValidacion("archivo", "Solo se aceptan imágenes JPG, JPEG o PNG."),
        };

        var firma = esperado == Jpeg ? FirmaJpeg : FirmaPng;
        if (!contenido.AsSpan().StartsWith(firma))
        {
            throw new ErrorValidacion("archivo", "El contenido del archivo no es una imagen JPG o PNG válida.");
        }

        return esperado;
    }
}
