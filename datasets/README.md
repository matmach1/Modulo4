# Dataset de validación de la clasificación

Casos etiquetados por el área de Seguridad e Higiene para medir la precisión de la clasificación
automática (SC-002, RNF-02, RNF-03) y la latencia de la llamada a Claude API (SC-003, RNF-01).

> Todavía no existe: lo entrega Seguridad e Higiene. Mientras no esté y no se alcancen los
> mínimos, el sistema solo puede usarse en un piloto (SC-002).

## Formato

Archivo CSV en UTF-8, con encabezado y comillas dobles cuando un campo tiene comas o saltos de
línea:

```csv
descripcion,ubicacion,tipo,categoria,criticidad
"Cable pelado, junto al tablero",Planta 1,Condición insegura,Riesgo eléctrico,Alta
```

Valores permitidos, escritos exactamente así:

| Columna | Valores |
|---------|---------|
| `tipo` | Acto inseguro · Condición insegura · Casi accidente · Riesgo operativo · Otro |
| `categoria` | Problema de infraestructura · Riesgo eléctrico · Uso de elementos de protección personal · Otro |
| `criticidad` | Baja · Media · Alta · Crítica |

Mínimos (PRD, RNF-02 y RNF-03): al menos 100 casos y al menos 10 por cada tipo, cada categoría y
cada nivel de criticidad. La herramienta avisa si no se cumplen.

## Versionado

- Cada versión es un archivo nuevo: `validacion-v1.csv`, `validacion-v2.csv`, … Nunca se edita una
  versión ya usada para medir.
- Los casos no deben incluir datos personales (nombres, legajos, emails).

## Cómo medir

Llama a Claude API real y tiene costo. Requiere `API_KEY` (y, opcionalmente, `CLAUDE_MODEL`):

```bash
cd backend
dotnet run --project tools/Riesgos.Evaluacion -- ../datasets/validacion-v1.csv
```

Los casos que quedan "Pendiente de clasificación" cuentan como error. La herramienta termina con
código 1 si no alcanza 90% en categoría y tipo, 85% en criticidad o p95 < 3 s.

Hay que volver a medir cada vez que cambian el modelo (`CLAUDE_MODEL`), las instrucciones del
clasificador o `backend/src/Riesgos.Clasificacion/kb.md`, y antes de cada salida a producción.
