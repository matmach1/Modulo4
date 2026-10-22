namespace Riesgos.Core.Dominio;

public enum Rol
{
    Empleado,
    Administrador,
}

public enum EstadoReporte
{
    Abierto,
    EnProgreso,
    Cerrado,
}

// "Pendiente" representa "Pendiente de clasificación" en los tres enums de clasificación.
public enum TipoReporte
{
    Pendiente,
    ActoInseguro,
    CondicionInsegura,
    CasiAccidente,
    RiesgoOperativo,
    Otro,
}

public enum Categoria
{
    Pendiente,
    ProblemaInfraestructura,
    RiesgoElectrico,
    UsoEpp,
    Otro,
}

public enum Criticidad
{
    Pendiente,
    Baja,
    Media,
    Alta,
    Critica,
}

public static class OrdenCriticidad
{
    // Orden del listado por criticidad (FR-024): Crítica primero y "Pendiente" al final.
    public static int Rango(Criticidad criticidad) => criticidad switch
    {
        Criticidad.Critica => 0,
        Criticidad.Alta => 1,
        Criticidad.Media => 2,
        Criticidad.Baja => 3,
        _ => 4,
    };
}
