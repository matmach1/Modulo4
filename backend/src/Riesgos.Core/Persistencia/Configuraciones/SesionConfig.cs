using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Riesgos.Core.Dominio;

namespace Riesgos.Core.Persistencia.Configuraciones;

public class SesionConfig : IEntityTypeConfiguration<Sesion>
{
    public void Configure(EntityTypeBuilder<Sesion> builder)
    {
        builder.Property(s => s.TokenHash).HasMaxLength(64);
        builder.HasIndex(s => s.TokenHash).IsUnique();
        builder.HasOne(s => s.Usuario).WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
