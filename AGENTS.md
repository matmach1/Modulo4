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
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | Administrador inicial (RF-30). Obligatorias si la base no tiene administrador; si ya existe, no lo modifican. En v1 hay un único administrador: para reemplazarlo hay que hacerlo en la base de datos (procedimiento a documentar con la implementación, T115). |
| `CLAUDE_MODEL` | Opcional. Modelo de clasificación; por defecto `claude-opus-5-5`. |

## Qué NO hacer
- No hardcodear la API key de Claude en el código: siempre desde la variable de entorno `API_KEY` (RNF-04).
- No guardar contraseñas en texto plano: siempre hash seguro con bcrypt/argon2 (RNF-05).
- No usar las imágenes adjuntas como input del modelo de clasificación: en v1 son solo evidencia visual para el administrador (RF-21).
- No implementar fuera de alcance: sin CRM completo, sin multi-canal real (mail/WhatsApp en vivo), sin RBAC configurable / más de dos roles, sin multi-tenant, sin envío real de mails.
