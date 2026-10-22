using System.Net.Http.Json;
using Riesgos.Api.Tests.Infraestructura;

namespace Riesgos.Api.Tests.US6;

public class HashTests
{
    [Fact]
    [Trait("AC", "AC-47")]
    [Trait("FR", "FR-007")]
    public async Task La_contrasena_se_guarda_como_hash_bcrypt_y_nunca_en_texto_plano()
    {
        await using var app = new ApiFactory();
        var email = Clientes.EmailNuevo();
        await app.CrearCliente().PostAsJsonAsync("/api/auth/registro", new { email, password = "Clave12345" });

        var guardado = app.EnScope(db => db.Usuarios.Single(u => u.Email == email).PasswordHash);

        Assert.NotEqual("Clave12345", guardado);
        Assert.DoesNotContain("Clave12345", guardado);
        Assert.Matches(@"^\$2[aby]\$\d{2}\$.{53}$", guardado);
    }
}
