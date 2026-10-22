import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import usePollingClasificacion from '../hooks/usePollingClasificacion';

const pendiente = () => ({ id: 1, tipo: 'Pendiente', categoria: 'Pendiente', criticidad: 'Pendiente' });
const clasificado = () => ({ id: 1, tipo: 'CasiAccidente', categoria: 'RiesgoElectrico', criticidad: 'Alta' });

describe('usePollingClasificacion', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('vuelve a consultar cada 2 s mientras el reporte esté pendiente', () => {
    const recargar = vi.fn();
    const { rerender } = renderHook(({ reporte }) => usePollingClasificacion(reporte, recargar), {
      initialProps: { reporte: pendiente() },
    });

    act(() => vi.advanceTimersByTime(2000));
    expect(recargar).toHaveBeenCalledTimes(1);

    rerender({ reporte: pendiente() });
    act(() => vi.advanceTimersByTime(2000));
    expect(recargar).toHaveBeenCalledTimes(2);
  });

  it('deja de consultar cuando llega la clasificación', () => {
    const recargar = vi.fn();
    const { rerender } = renderHook(({ reporte }) => usePollingClasificacion(reporte, recargar), {
      initialProps: { reporte: pendiente() },
    });

    rerender({ reporte: clasificado() });
    act(() => vi.advanceTimersByTime(10000));

    expect(recargar).not.toHaveBeenCalled();
  });

  it('deja de consultar a los 15 s aunque siga pendiente', () => {
    const recargar = vi.fn();
    const { rerender } = renderHook(({ reporte }) => usePollingClasificacion(reporte, recargar), {
      initialProps: { reporte: pendiente() },
    });

    for (let i = 0; i < 20; i += 1) {
      act(() => vi.advanceTimersByTime(2000));
      rerender({ reporte: pendiente() });
    }

    expect(recargar).toHaveBeenCalledTimes(7);
  });
});
