# Quickstart: validar el Sistema de Reporte de Riesgos Laborales

**Plan**: [plan.md](plan.md) · **API**: [contracts/api.md](contracts/api.md) · **Datos**: [data-model.md](data-model.md)

## Prerrequisitos

- .NET 8 SDK, Node 20 LTS.
- Variables de entorno (nunca en archivos versionados, AC-46):

  | Variable | Uso |
  |----------|-----|
  | `API_KEY` | Clave de Claude API (RNF-04). Sin ella, los reportes quedan "Pendiente de clasificación". |
  | `ADMIN_EMAIL`, `ADMIN_PASSWORD` | Administrador inicial (FR-006). Obligatorias si la base no tiene administrador. |
  | `CLAUDE_MODEL` | Opcional; por defecto `claude-opus-5-5`. |

## 1. Tests automatizados (sin llamar a Claude API)

```bash
cd backend && dotnet test
cd frontend && npm test
```

Esperado: todo en verde. Los tests usan el clasificador simulado y cubren los AC del PRD, cada uno
referenciado por su ID en el nombre o atributo del test (Principio IV).

## 2. Levantar la aplicación

```bash
cd backend && dotnet restore && dotnet run --project src/Riesgos.Api
cd frontend && npm install && npm run dev
```

Abrir la URL que muestra Vite. El proxy de Vite envía `/api` al backend.

## 3. Recorrido manual

1. **Registro y reporte (US1)**: registrarse con un email nuevo y una contraseña de 8+
   caracteres, crear un reporte con descripción y ubicación. Esperado: aparece en "Mis reportes"
   en estado Abierto, con "Pendiente de clasificación" al principio y la clasificación unos
   segundos después (US2).
2. **Acceso ajeno (US1)**: con un segundo empleado, abrir la URL del reporte del primero.
   Esperado: acceso denegado (403).
3. **Panel y listado (US3)**: entrar como administrador. Esperado: el panel muestra los contadores
   de Críticos y pendientes en estado Abierto; el listado admite orden por fecha o por criticidad;
   el detalle muestra acción sugerida y borrador de mail.
4. **Gestión (US4)**: pasar el reporte a En Progreso, registrar una acción, intentar cerrar sin
   textos (rechazado) y cerrar con ambos. Esperado: Cerrado con fecha, hora y usuario de cierre.
5. **Edición e imágenes (US5)**: con un reporte Abierto propio, editarlo (vuelve a "Pendiente" y
   se reclasifica), adjuntar una imagen JPG, intentar un archivo `.txt` renombrado a `.jpg`
   (rechazado) y una sexta imagen (rechazada).
6. **Sesión (US6)**: cerrar sesión y repetir la última request con la cookie anterior. Esperado:
   401.
7. **Falla de la IA (US2)**: reiniciar el backend sin `API_KEY` y crear un reporte. Esperado: se
   crea (201) y queda "Pendiente de clasificación"; el contador de pendientes del panel sube.

## 4. Precisión y latencia (SC-002, SC-003)

Requiere el dataset de Seguridad e Higiene (todavía no existe) y llama a Claude API real (tiene
costo):

```bash
cd backend && dotnet run --project tools/Riesgos.Evaluacion -- ../datasets/validacion-v1.csv
```

Esperado: aciertos ≥ 90% en categoría y tipo, ≥ 85% en criticidad, y p95 de latencia < 3 s
(AC-05, AC-45). Los casos "Pendiente" cuentan como error.

## 5. Chequeo de secretos (AC-46)

```bash
scripts/verificar-secretos.sh   # falla si encuentra secretos en archivos versionados (T117)
```
