using Riesgos.Core.Imagenes;
using Riesgos.Core.Validacion;

namespace Riesgos.Core.Tests.Imagenes;

public class ValidadorImagenTests
{
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Theory]
    [Trait("FR", "FR-013")]
    [InlineData("foto.jpg")]
    [InlineData("foto.JPEG")]
    public void Jpeg_con_firma_valida_se_acepta(string nombre)
    {
        var resultado = ValidadorImagen.Validar(nombre, Jpeg);

        Assert.Equal("image/jpeg", resultado.TipoContenido);
        Assert.Equal(".jpg", resultado.Extension);
    }

    [Fact]
    [Trait("FR", "FR-013")]
    public void Png_con_firma_valida_se_acepta()
    {
        var resultado = ValidadorImagen.Validar("plano.png", Png);

        Assert.Equal("image/png", resultado.TipoContenido);
        Assert.Equal(".png", resultado.Extension);
    }

    [Theory]
    [Trait("AC", "AC-20")]
    [InlineData("notas.txt")]
    [InlineData("foto.gif")]
    [InlineData("foto")]
    [InlineData(null)]
    public void Extension_no_permitida_es_invalida(string? nombre)
    {
        Assert.Throws<ErrorValidacion>(() => ValidadorImagen.Validar(nombre, Jpeg));
    }

    [Fact]
    [Trait("AC", "AC-20")]
    public void Un_texto_renombrado_a_jpg_es_invalido()
    {
        Assert.Throws<ErrorValidacion>(() => ValidadorImagen.Validar("foto.jpg", "hola, no soy una imagen"u8.ToArray()));
    }

    [Fact]
    [Trait("AC", "AC-20")]
    public void Extension_y_contenido_que_no_coinciden_son_invalidos()
    {
        Assert.Throws<ErrorValidacion>(() => ValidadorImagen.Validar("foto.png", Jpeg));
    }

    [Fact]
    [Trait("AC", "AC-21")]
    public void Mas_de_2_MB_es_invalido_y_exactamente_2_MB_se_acepta()
    {
        var limite = new byte[ValidadorImagen.TamanoMaximo];
        Jpeg.CopyTo(limite, 0);
        var excedido = new byte[ValidadorImagen.TamanoMaximo + 1];
        Jpeg.CopyTo(excedido, 0);

        Assert.Equal("image/jpeg", ValidadorImagen.Validar("grande.jpg", limite).TipoContenido);
        Assert.Throws<ErrorValidacion>(() => ValidadorImagen.Validar("enorme.jpg", excedido));
    }

    [Fact]
    public void Un_archivo_vacio_es_invalido()
    {
        Assert.Throws<ErrorValidacion>(() => ValidadorImagen.Validar("vacia.png", []));
    }
}
