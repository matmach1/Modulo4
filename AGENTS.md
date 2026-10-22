# AGENTS.md

## Propósito
Aplicación web donde empleados reportan actos/condiciones inseguras, casi accidentes y otros riesgos laborales. El sistema clasifica cada reporte por categoría y criticidad usando la API de Claude, y permite a un administrador gestionarlo hasta su cierre con acciones correctivas.

## Stack
- Backend: .NET 8 (C#), ASP.NET Core Web API
- Datos: SQLite vía EF Core
- Tests backend: xUnit (`dotnet test`)
- Frontend: React 18 + Vite 5, Node 20 LTS
- Clasificación/criticidad: Claude API

## Cómo correr

Backend (desde `backend/`):
```
dotnet restore
dotnet run --project src/Riesgos.Api
dotnet test
```

Frontend (desde `frontend/`):
```
npm install
npm run dev
npm test
```

## Variables de entorno
Nunca se versionan con valores reales (`.env` va en `.gitignore`).

| Variable | Uso |
|----------|-----|
| `API_KEY` | Clave de Claude API (RNF-04). Sin ella la app arranca, pero los reportes quedan "Pendiente de clasificación". |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | Administrador inicial (RF-30). Obligatorias si la base no tiene administrador; si ya existe, no lo modifican. En v1 hay un único administrador: ver "Reemplazar al administrador". |
| `CLAUDE_MODEL` | Opcional. Modelo de clasificación; por defecto `claude-opus-5-5`. |

## Verificaciones

Desde la raíz del repo:
```
scripts/verificar-cobertura-ac.sh   # cada AC del PRD tiene al menos un test
scripts/verificar-secretos.sh       # no hay secretos en archivos versionados (AC-46)
```

Precisión y latencia de la clasificación (SC-002, SC-003) contra Claude API real; tiene costo y
no corre en `dotnet test`. Formato del dataset en `datasets/README.md`. Desde `backend/`:
```
dotnet run --project tools/Riesgos.Evaluacion -- ../datasets/validacion-v1.csv
```
Volver a medir al cambiar `CLAUDE_MODEL`, las instrucciones del clasificador o `kb.md`.

## Reemplazar al administrador

La app no permite dar de alta administradores (v1 tiene uno solo). Para reemplazarlo sin perder
el historial (lo que cerró o registró sigue a su nombre):

1. Hacer una copia de `data/riesgos.db` y detener la app.
2. Deshabilitar la cuenta actual (deja de poder iniciar sesión y pasa a rol empleado):
   ```
   sqlite3 data/riesgos.db "DELETE FROM Sesiones WHERE UsuarioId IN (SELECT Id FROM Usuarios WHERE Rol = 'Administrador'); UPDATE Usuarios SET Rol = 'Empleado', PasswordHash = 'deshabilitado' WHERE Rol = 'Administrador';"
   ```
3. Definir `ADMIN_EMAIL` y `ADMIN_PASSWORD` del nuevo administrador y arrancar la app: al no
   haber administrador, lo crea. El nuevo email no puede ser el de una cuenta existente.

## Qué NO hacer
- No hardcodear la API key de Claude en el código: siempre desde la variable de entorno `API_KEY` (RNF-04).
- No guardar contraseñas en texto plano: siempre hash seguro con bcrypt/argon2 (RNF-05).
- No usar las imágenes adjuntas como input del modelo de clasificación: en v1 son solo evidencia visual para el administrador (RF-21).
- No implementar fuera de alcance: sin CRM completo, sin multi-canal real (mail/WhatsApp en vivo), sin RBAC configurable / más de dos roles, sin multi-tenant, sin envío real de mails.
