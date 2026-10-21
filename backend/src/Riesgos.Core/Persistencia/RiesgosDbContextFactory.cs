using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Riesgos.Core.Persistencia;

// Solo para las herramientas de EF Core (crear migraciones); la app configura el contexto en Program.cs.
public class RiesgosDbContextFactory : IDesignTimeDbContextFactory<RiesgosDbContext>
{
    public RiesgosDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<RiesgosDbContext>().UseSqlite("Data Source=design.db").Options);
}
