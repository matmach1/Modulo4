using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Riesgos.Core.Clasificacion;

namespace Riesgos.Clasificacion;

public static class RegistroServicios
{
    // La API registra el clasificador sin conocer el SDK de Claude (Principio I).
    public static IServiceCollection AddClasificadorClaude(this IServiceCollection servicios)
    {
        servicios.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var modelo = config["CLAUDE_MODEL"];
            return new ConfiguracionClasificador(
                config["API_KEY"],
                string.IsNullOrWhiteSpace(modelo) ? ClasificadorClaude.ModeloPorDefecto : modelo,
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "kb.md")));
        });
        servicios.AddSingleton<IClasificadorReportes, ClasificadorClaude>();
        return servicios;
    }
}
