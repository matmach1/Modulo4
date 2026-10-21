using Riesgos.Core.Dominio;

namespace Riesgos.Core.Tests;

// Principio I de la constitución: solo Riesgos.Clasificacion usa el SDK de Claude.
public class ArquitecturaTests
{
    [Fact]
    [Trait("FR", "FR-014")]
    public void Core_no_referencia_el_SDK_de_Anthropic()
    {
        var referencias = typeof(Usuario).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencias, r => r.Name!.StartsWith("Anthropic", StringComparison.Ordinal));
    }
}
