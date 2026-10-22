using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Riesgos.Api.Tests.Infraestructura;
using Riesgos.Core.Dominio;

namespace Riesgos.Api.Tests.US3;

public class ListadoAdminTests
{
    private static async Task<PaginaDto<ReporteDto>> ListarAsync(HttpClient admin, string consulta = "") =>
        (await admin.GetFromJsonAsync<PaginaDto<ReporteDto>>($"/api/admin/reportes{consulta}"))!;

    private static async Task CrearVariosAsync(ApiFactory app, HttpClient cliente, int cantidad, bool avanzarReloj = true)
    {
        for (var i = 0; i < cantidad; i++)
        {
            await cliente.CrearReporteAsync($"Reporte {i}");
            if (avanzarReloj)
            {
                app.Reloj.Advance(TimeSpan.FromMinutes(1));
            }
        }
    }

    [Fact]
    [Trait("AC", "AC-02")]
    public async Task Devuelve_como_maximo_size_reportes_de_la_pagina_pedida_con_su_estado()
    {
        await using var app = new ApiFactory();
        var a = await app.ClienteAutenticadoAsync();
        var b = await app.ClienteAutenticadoAsync();
        await CrearVariosAsync(app, a, 3);
        await CrearVariosAsync(app, b, 2);
        var admin = await app.ComoAdminAsync();

        var primera = await ListarAsync(admin, "?page=1&size=2");
        var tercera = await ListarAsync(admin, "?page=3&size=2");

        Assert.Equal(5, primera.Total);
        Assert.Equal(2, primera.Items.Count);
        Assert.Single(tercera.Items);
        Assert.All(primera.Items, r => Assert.Equal("Abierto", r.Estado));
    }

    [Fact]
    [Trait("AC", "AC-49")]
    public async Task Ordena_del_mas_reciente_al_mas_antiguo()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        await CrearVariosAsync(app, cliente, 3);
        var admin = await app.ComoAdminAsync();

        var pagina = await ListarAsync(admin);

        Assert.Equal(["Reporte 2", "Reporte 1", "Reporte 0"], pagina.Items.Select(r => r.Descripcion));
    }

    [Fact]
    [Trait("FR", "FR-024")]
    public async Task Con_la_misma_fecha_el_orden_es_estable_por_id_descendente()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        await CrearVariosAsync(app, cliente, 3, avanzarReloj: false);
        var admin = await app.ComoAdminAsync();

        var ids = (await ListarAsync(admin)).Items.Select(r => r.Id).ToList();

        Assert.Equal(ids.OrderByDescending(id => id), ids);
    }

    [Fact]
    [Trait("FR", "FR-024")]
    public async Task Pagina_fuera_de_rango_devuelve_lista_vacia_y_size_por_defecto_es_20()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        await CrearVariosAsync(app, cliente, 21, avanzarReloj: false);
        var admin = await app.ComoAdminAsync();

        var porDefecto = await ListarAsync(admin);
        var fuera = await ListarAsync(admin, "?page=10");

        Assert.Equal(20, porDefecto.Size);
        Assert.Equal(20, porDefecto.Items.Count);
        Assert.Empty(fuera.Items);
        Assert.Equal(21, fuera.Total);
    }

    [Theory]
    [Trait("FR", "FR-024")]
    [InlineData("?size=0")]
    [InlineData("?size=101")]
    [InlineData("?size=-5")]
    [InlineData("?page=0")]
    public async Task Size_o_page_fuera_de_rango_responde_400(string consulta)
    {
        await using var app = new ApiFactory();
        var admin = await app.ComoAdminAsync();

        var respuesta = await admin.GetAsync($"/api/admin/reportes{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-03")]
    public async Task Un_empleado_recibe_403()
    {
        await using var app = new ApiFactory();
        var empleado = await app.ClienteAutenticadoAsync();

        var respuesta = await empleado.GetAsync("/api/admin/reportes");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-42")]
    public async Task Un_usuario_recien_registrado_recibe_403()
    {
        await using var app = new ApiFactory();
        var cliente = app.CrearCliente();
        var email = Clientes.EmailNuevo();
        await cliente.PostAsJsonAsync("/api/auth/registro", new { email, password = "Clave12345" });
        await cliente.LoginAsync(email, "Clave12345");

        var respuesta = await cliente.GetAsync("/api/admin/reportes");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-12")]
    public async Task Sin_sesion_responde_401()
    {
        await using var app = new ApiFactory();

        var respuesta = await app.CrearCliente().GetAsync("/api/admin/reportes");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-44")]
    [Trait("FR", "FR-006")]
    public async Task Con_base_vacia_el_administrador_del_entorno_puede_entrar_y_listar()
    {
        await using var app = new ApiFactory();
        var cliente = app.CrearCliente();

        var login = await cliente.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var listado = await cliente.GetAsync("/api/admin/reportes");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
    }

    [Fact]
    [Trait("AC", "AC-70")]
    [Trait("FR", "FR-024")]
    public async Task Ordenado_por_criticidad_primero_Critica_y_al_final_Pendiente_con_fecha_desc_en_cada_nivel()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        var criticidades = new[]
        {
            Criticidad.Baja, Criticidad.Critica, Criticidad.Pendiente, Criticidad.Alta, Criticidad.Media, Criticidad.Critica,
        };
        var ids = new List<int>();
        foreach (var criticidad in criticidades)
        {
            var id = (await cliente.CrearReporteAsync($"{criticidad} {ids.Count}")).Id;
            app.EnScope(db => db.Reportes.Where(r => r.Id == id).ExecuteUpdate(s => s.SetProperty(r => r.Criticidad, criticidad)));
            ids.Add(id);
            app.Reloj.Advance(TimeSpan.FromMinutes(1));
        }

        var admin = await app.ComoAdminAsync();
        var pagina = await ListarAsync(admin, "?orden=criticidad");

        Assert.Equal(["Critica 5", "Critica 1", "Alta 3", "Media 4", "Baja 0", "Pendiente 2"], pagina.Items.Select(r => r.Descripcion));
    }

    [Fact]
    [Trait("AC", "AC-71")]
    public async Task Criterio_de_orden_desconocido_responde_400()
    {
        await using var app = new ApiFactory();
        var admin = await app.ComoAdminAsync();

        var respuesta = await admin.GetAsync("/api/admin/reportes?orden=prioridad");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    [Trait("FR", "FR-024")]
    public async Task Orden_fecha_explicito_equivale_al_orden_por_defecto()
    {
        await using var app = new ApiFactory();
        var cliente = await app.ClienteAutenticadoAsync();
        await CrearVariosAsync(app, cliente, 3);
        var admin = await app.ComoAdminAsync();

        var explicito = await ListarAsync(admin, "?orden=fecha");
        var porDefecto = await ListarAsync(admin);

        Assert.Equal(porDefecto.Items.Select(r => r.Id), explicito.Items.Select(r => r.Id));
    }
}
