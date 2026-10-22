#!/usr/bin/env bash
# AC-46: ningún secreto en archivos versionados. Termina con código 1 si encuentra alguno.
set -euo pipefail
cd "$(dirname "$0")/.."

problemas=0
avisar() { echo "SECRETO: $1"; problemas=1; }

# 1. Archivos .env versionados (solo se admite .env.example, sin valores).
while IFS= read -r archivo; do
  avisar "archivo de entorno versionado: $archivo"
done < <(git ls-files | grep -E '(^|/)\.env($|\.)' | grep -vE '(^|/)\.env\.example$' || true)

if git ls-files --error-unmatch .env.example >/dev/null 2>&1 && grep -qE '^[A-Z_]+=.+' .env.example; then
  avisar ".env.example tiene valores (debe tener solo los nombres)"
fi

# 2. Claves con formato de Anthropic.
if git grep -nIE 'sk-ant-[A-Za-z0-9_-]{10,}' -- . ':!scripts/verificar-secretos.sh' >/dev/null; then
  avisar "posible clave de Anthropic (sk-ant-…):"
  git grep -nIE 'sk-ant-[A-Za-z0-9_-]{10,}' -- . ':!scripts/verificar-secretos.sh' | cut -c1-120
fi

# 3. Valores reales del entorno actual, si están definidos.
for variable in API_KEY ADMIN_PASSWORD; do
  valor="${!variable:-}"
  if [[ ${#valor} -ge 8 ]] && git grep -nIF -- "$valor" >/dev/null; then
    avisar "el valor de $variable aparece en:"
    git grep -lIF -- "$valor"
  fi
done

if [[ $problemas -eq 0 ]]; then
  echo "OK: no se encontraron secretos en archivos versionados."
fi
exit $problemas
