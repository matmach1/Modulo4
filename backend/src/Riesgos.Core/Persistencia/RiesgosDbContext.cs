using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Persistencia;

public class RiesgosDbContext(DbContextOptions<RiesgosDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Sesion> Sesiones => Set<Sesion>();

    public DbSet<Reporte> Reportes => Set<Reporte>();

    // Cada modificación de un reporte cambia su token de concurrencia (research R7): si otra
    // operación lo modificó antes, EF Core lanza DbUpdateConcurrencyException (HTTP 409).
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entrada in ChangeTracker.Entries<Reporte>().Where(e => e.State == EntityState.Modified))
        {
            entrada.Entity.RowVersion++;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RiesgosDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite no ordena ni compara DateTimeOffset; guardado como entero (siempre UTC) sí.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }
}
