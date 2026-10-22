using Riesgos.Core.Validacion;

namespace Riesgos.Core.Tests.Validacion;

public class TextoTests
{
    [Theory]
    [InlineData("  hola  ", "hola")]
    [InlineData("hola", "hola")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalizar_recorta_espacios(string? entrada, string esperado)
    {
        Assert.Equal(esperado, Texto.Normalizar(entrada));
    }

    [Fact]
    public void Validar_devuelve_el_texto_recortado()
    {
        Assert.Equal("Pasillo B", Texto.Validar("  Pasillo B ", "ubicacion", 200));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Validar_rechaza_vacio_o_solo_espacios(string? valor)
    {
        var error = Assert.Throws<ErrorValidacion>(() => Texto.Validar(valor, "descripcion", 2000));
        Assert.Equal("descripcion", error.Campo);
    }

    [Fact]
    public void Validar_acepta_exactamente_el_maximo()
    {
        var valor = new string('a', 200);
        Assert.Equal(valor, Texto.Validar(valor, "ubicacion", 200));
    }

    [Fact]
    public void Validar_rechaza_mas_del_maximo()
    {
        var error = Assert.Throws<ErrorValidacion>(() => Texto.Validar(new string('a', 201), "ubicacion", 200));
        Assert.Equal("ubicacion", error.Campo);
    }

    [Fact]
    public void Validar_mide_el_largo_despues_de_recortar()
    {
        var valor = " " + new string('a', 200) + " ";
        Assert.Equal(200, Texto.Validar(valor, "ubicacion", 200).Length);
    }
}
