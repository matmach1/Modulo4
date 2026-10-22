using Riesgos.Core.Servicios;

namespace Riesgos.Core.Tests.Servicios;

public class HashContrasenaTests
{
    private readonly HashContrasena _hash = new(factorTrabajo: 4);

    [Fact]
    [Trait("FR", "FR-007")]
    public void Verifica_la_contrasena_correcta_y_rechaza_otra()
    {
        var guardado = _hash.Hash("Clave12345");

        Assert.True(_hash.Verificar("Clave12345", guardado));
        Assert.False(_hash.Verificar("OtraClave1", guardado));
    }

    [Theory]
    [Trait("FR", "FR-006")]
    [InlineData("deshabilitado")]
    [InlineData("")]
    public void Un_hash_con_formato_invalido_no_valida_y_no_lanza(string guardado)
    {
        // Así se deshabilita la cuenta de un administrador reemplazado (AGENTS.md).
        Assert.False(_hash.Verificar("cualquiera", guardado));
    }
}
