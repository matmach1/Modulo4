#!/usr/bin/env bash
# Verifica que cada AC del PRD tenga al menos un test con [Trait("AC", "AC-xx")] (Principio IV).
# AC-05 y AC-45 se miden con tools/Riesgos.Evaluacion; AC-46 con scripts/verificar-secretos.sh.
set -euo pipefail
cd "$(dirname "$0")/.."

prd=$(grep -oE '^- AC-[0-9]+' PRD.md | cut -c3- | sort -u)
tests=$(grep -rhoE 'Trait\("AC", "AC-[0-9]+"\)' backend/tests | grep -oE 'AC-[0-9]+' | sort -u)
faltan=$(comm -23 <(echo "$prd") <(echo "$tests") | grep -vxE 'AC-05|AC-45|AC-46' || true)

if [[ -n "$faltan" ]]; then
  echo "AC del PRD sin test:"
  echo "$faltan"
  exit 1
fi

echo "OK: los $(echo "$prd" | wc -l) AC del PRD tienen test (AC-05, AC-45 y AC-46 se validan aparte)."
