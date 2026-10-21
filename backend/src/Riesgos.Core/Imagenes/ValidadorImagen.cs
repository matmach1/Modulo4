namespace Riesgos.Core.Imagenes;

public sealed record TipoImagen(string TipoContenido, string Extension);

public static class ValidadorImagen
{
    public const int TamanoMaximo = 2 * 1024 * 1024;

    // Esqueleto: se implementa en T095.
    public static TipoImagen Validar(string? nombreArchivo, byte[] contenido) => throw new NotImplementedException();
}
