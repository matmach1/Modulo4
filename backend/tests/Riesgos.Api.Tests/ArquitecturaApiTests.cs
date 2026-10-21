namespace Riesgos.Api.Tests;

// Principio I: la API registra el clasificador a través de Riesgos.Clasificacion, sin usar el SDK.
public class ArquitecturaApiTests
{
    [Fact]
    [Trait("FR", "FR-014")]
    public void Api_no_referencia_el_SDK_de_Anthropic()
    {
        var referencias = typeof(Program).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencias, r => r.Name!.StartsWith("Anthropic", StringComparison.Ordinal));
    }
}
