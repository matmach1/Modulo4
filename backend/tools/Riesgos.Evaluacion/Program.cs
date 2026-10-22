using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Riesgos.Clasificacion;
using Riesgos.Core.Clasificacion;
using Riesgos.Core.Dominio;

// Mide precisión (SC-002, AC-05) y latencia p95 (SC-003, AC-45) contra Claude API real.
// No forma parte de `dotnet test`: llama a la API real y tiene costo (research R9).
// Uso: dotnet run --project tools/Riesgos.Evaluacion -- <dataset.csv>
// CSV con encabezado: descripcion,ubicacion,tipo,categoria,criticidad (valores con los textos de la spec).

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Uso: dotnet run --project tools/Riesgos.Evaluacion -- <dataset.csv>");
    return 2;
}

var casos = LeerDataset(args[0]);
AdvertirSiElDatasetEsChico(casos);

var modelo = Environment.GetEnvironmentVariable("CLAUDE_MODEL");
var clasificador = new ClasificadorClaude(
    new ConfiguracionClasificador(
        Environment.GetEnvironmentVariable("API_KEY"),
        string.IsNullOrWhiteSpace(modelo) ? ClasificadorClaude.ModeloPorDefecto : modelo,
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "kb.md"))),
    NullLogger<ClasificadorClaude>.Instance);

int aciertosTipo = 0, aciertosCategoria = 0, aciertosCriticidad = 0, pendientes = 0;
var latencias = new List<double>();
for (var i = 0; i < casos.Count; i++)
{
    var caso = casos[i];
    using var limite = new CancellationTokenSource(ProcesadorClasificacion.Limite);
    var reloj = Stopwatch.StartNew();
    ResultadoClasificacion resultado;
    try
    {
        resultado = await clasificador.ClasificarAsync(new EntradaClasificacion(caso.Descripcion, caso.Ubicacion, null), limite.Token);
    }
    catch (OperationCanceledException)
    {
        resultado = new ResultadoClasificacion.Falla(MotivoFalla.Timeout);
    }

    latencias.Add(reloj.Elapsed.TotalSeconds);

    // Un caso que queda "Pendiente de clasificación" cuenta como error en los tres campos.
    if (resultado is ResultadoClasificacion.Exito exito)
    {
        aciertosTipo += exito.Tipo == caso.Tipo ? 1 : 0;
        aciertosCategoria += exito.Categoria == caso.Categoria ? 1 : 0;
        aciertosCriticidad += exito.Criticidad == caso.Criticidad ? 1 : 0;
    }
    else
    {
        pendientes++;
    }

    Console.Write($"\r{i + 1}/{casos.Count}");
}

Console.WriteLine();
var p95 = Percentil(latencias, 0.95);
var resultados = new[]
{
    ("Categoría", Porcentaje(aciertosCategoria, casos.Count), 90.0),
    ("Tipo", Porcentaje(aciertosTipo, casos.Count), 90.0),
    ("Criticidad", Porcentaje(aciertosCriticidad, casos.Count), 85.0),
};

Console.WriteLine($"Modelo: {(string.IsNullOrWhiteSpace(modelo) ? ClasificadorClaude.ModeloPorDefecto : modelo)} · Casos: {casos.Count} · Pendientes: {pendientes}");
var aprobado = true;
foreach (var (campo, porcentaje, minimo) in resultados)
{
    var ok = porcentaje >= minimo;
    aprobado &= ok;
    Console.WriteLine($"{campo,-11} {porcentaje,6:F1}%  (mínimo {minimo}%)  {(ok ? "OK" : "NO ALCANZA")}");
}

var latenciaOk = p95 < 3.0;
aprobado &= latenciaOk;
Console.WriteLine($"Latencia p95 {p95,6:F2} s  (máximo 3 s)    {(latenciaOk ? "OK" : "NO ALCANZA")}");
Console.WriteLine(aprobado
    ? "Resultado: cumple SC-002 y SC-003."
    : "Resultado: NO cumple; según SC-002 el sistema solo puede usarse en un piloto.");
return aprobado ? 0 : 1;

static List<Caso> LeerDataset(string ruta)
{
    var filas = LeerCsv(File.ReadAllText(ruta, Encoding.UTF8));
    var encabezado = filas[0].Select(c => c.Trim().ToLowerInvariant()).ToList();
    int Columna(string nombre) => encabezado.IndexOf(nombre) is var i and >= 0
        ? i
        : throw new InvalidDataException($"Falta la columna '{nombre}' en el encabezado.");

    var (d, u, t, c, k) = (Columna("descripcion"), Columna("ubicacion"), Columna("tipo"), Columna("categoria"), Columna("criticidad"));
    return filas.Skip(1)
        .Where(f => f.Count > 1)
        .Select((f, n) => new Caso(
            f[d],
            f[u],
            Valor(MapeadorRespuesta.Tipos, f[t], n),
            Valor(MapeadorRespuesta.Categorias, f[c], n),
            Valor(MapeadorRespuesta.Criticidades, f[k], n)))
        .ToList();
}

static T Valor<T>(IReadOnlyDictionary<string, T> valores, string texto, int fila) =>
    valores.TryGetValue(texto.Trim(), out var valor)
        ? valor
        : throw new InvalidDataException($"Fila {fila + 2}: '{texto}' no es un valor válido ({string.Join(", ", valores.Keys)}).");

// CSV con comillas dobles (RFC 4180): las descripciones pueden tener comas y saltos de línea.
static List<List<string>> LeerCsv(string texto)
{
    var filas = new List<List<string>>();
    var fila = new List<string>();
    var campo = new StringBuilder();
    var entreComillas = false;
    for (var i = 0; i < texto.Length; i++)
    {
        var ch = texto[i];
        if (entreComillas)
        {
            if (ch == '"' && i + 1 < texto.Length && texto[i + 1] == '"')
            {
                campo.Append('"');
                i++;
            }
            else if (ch == '"')
            {
                entreComillas = false;
            }
            else
            {
                campo.Append(ch);
            }
        }
        else if (ch == '"')
        {
            entreComillas = true;
        }
        else if (ch == ',')
        {
            fila.Add(campo.ToString());
            campo.Clear();
        }
        else if (ch == '\n')
        {
            fila.Add(campo.ToString().TrimEnd('\r'));
            campo.Clear();
            filas.Add(fila);
            fila = [];
        }
        else
        {
            campo.Append(ch);
        }
    }

    if (campo.Length > 0 || fila.Count > 0)
    {
        fila.Add(campo.ToString());
        filas.Add(fila);
    }

    return filas;
}

// RNF-02/RNF-03: al menos 100 casos y 10 por cada tipo, categoría y nivel de criticidad.
static void AdvertirSiElDatasetEsChico(List<Caso> casos)
{
    var avisos = new List<string>();
    if (casos.Count < 100)
    {
        avisos.Add($"tiene {casos.Count} casos (mínimo 100)");
    }

    void Revisar<T>(string nombre, IReadOnlyDictionary<string, T> valores, Func<Caso, T> campo)
        where T : notnull
    {
        foreach (var (texto, valor) in valores)
        {
            var cantidad = casos.Count(c => campo(c).Equals(valor));
            if (cantidad < 10)
            {
                avisos.Add($"{nombre} '{texto}' tiene {cantidad} casos (mínimo 10)");
            }
        }
    }

    Revisar("tipo", MapeadorRespuesta.Tipos, c => c.Tipo);
    Revisar("categoría", MapeadorRespuesta.Categorias, c => c.Categoria);
    Revisar("criticidad", MapeadorRespuesta.Criticidades, c => c.Criticidad);
    foreach (var aviso in avisos)
    {
        Console.WriteLine($"Aviso: el dataset {aviso}; el resultado no vale como validación de SC-002.");
    }
}

static double Porcentaje(int aciertos, int total) => total == 0 ? 0 : 100.0 * aciertos / total;

static double Percentil(List<double> valores, double p)
{
    if (valores.Count == 0)
    {
        return 0;
    }

    var ordenados = valores.Order().ToList();
    var indice = (int)Math.Ceiling(p * ordenados.Count) - 1;
    return ordenados[Math.Clamp(indice, 0, ordenados.Count - 1)];
}

internal sealed record Caso(string Descripcion, string Ubicacion, TipoReporte Tipo, Categoria Categoria, Criticidad Criticidad);
