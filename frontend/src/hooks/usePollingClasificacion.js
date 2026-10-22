import { useEffect, useRef } from 'react';

export function estaPendiente(reporte) {
  return [reporte.tipo, reporte.categoria, reporte.criticidad].includes('Pendiente');
}

// La clasificación se hace en segundo plano (FR-014): mientras el reporte esté pendiente,
// vuelve a consultarlo cada 2 s durante 15 s como máximo (research R11).
export default function usePollingClasificacion(reporte, recargar, intervaloMs = 2000, maximoMs = 15000) {
  const inicio = useRef(null);

  useEffect(() => {
    if (!reporte) return undefined;
    if (!estaPendiente(reporte)) {
      inicio.current = null;
      return undefined;
    }
    if (inicio.current === null) inicio.current = Date.now();
    if (Date.now() - inicio.current + intervaloMs > maximoMs) return undefined;

    const temporizador = setTimeout(recargar, intervaloMs);
    return () => clearTimeout(temporizador);
  }, [reporte, recargar, intervaloMs, maximoMs]);
}
