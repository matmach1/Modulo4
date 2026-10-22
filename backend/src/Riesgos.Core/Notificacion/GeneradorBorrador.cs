using System.Globalization;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Notificacion;

public sealed record BorradorMail(string Asunto, string Cuerpo);

// Borrador de mail para copiar y pegar (FR-021): no se envían mails reales (FR-023).
public static class GeneradorBorrador
{
    public static BorradorMail Generar(Reporte reporte, string emailCreador)
    {
        var pendiente = reporte.Categoria == Categoria.Pendiente || reporte.Criticidad == Criticidad.Pendiente;
        var asunto = pendiente
            ? $"[{Textos.Pendiente}] Reporte #{reporte.Id}"
            : $"[{Textos.Categorias[reporte.Categoria]}] Criticidad {Textos.Criticidades[reporte.Criticidad]} - Reporte #{reporte.Id}";

        var fecha = reporte.FechaCreacion.ToUniversalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        var cuerpo = $"""
            Se registró un nuevo reporte de riesgo laboral.

            Descripción: {reporte.Descripcion}
            Ubicación: {reporte.Ubicacion}
            Tipo: {Textos.Tipos[reporte.Tipo]}
            Categoría: {Textos.Categorias[reporte.Categoria]}
            Criticidad: {Textos.Criticidades[reporte.Criticidad]}
            Fecha de creación: {fecha} UTC
            Creado por: {emailCreador}
            """;
        return new BorradorMail(asunto, cuerpo);
    }
}
