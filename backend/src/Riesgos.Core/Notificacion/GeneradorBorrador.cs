using Riesgos.Core.Dominio;

namespace Riesgos.Core.Notificacion;

public sealed record BorradorMail(string Asunto, string Cuerpo);

public static class GeneradorBorrador
{
    // Esqueleto: se implementa en T066.
    public static BorradorMail Generar(Reporte reporte, string emailCreador) => throw new NotImplementedException();
}
