import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ADMIN, EMPLEADO, renderApp, respuestaJson, sesionDe, simularApi } from './utils';

const DETALLE = {
  id: 3,
  descripcion: 'Tablero abierto',
  ubicacion: 'Depósito',
  estado: 'Abierto',
  tipo: 'CondicionInsegura',
  categoria: 'ProblemaInfraestructura',
  criticidad: 'Critica',
  categoriaSugerida: 'RiesgoElectrico',
  fechaCreacion: '2026-10-21T12:00:00Z',
  creadorEmail: EMPLEADO.email,
  inicioProgreso: null,
  cierre: null,
  accionSugerida: 'Detener actividad',
  borradorMail: { asunto: '[Problema de infraestructura] Criticidad Crítica - Reporte #3', cuerpo: 'Descripción: Tablero abierto' },
};

describe('Panel', () => {
  it('muestra los contadores, también en cero', async () => {
    simularApi([sesionDe(ADMIN), ['GET', '/api/admin/panel', () => respuestaJson(200, { criticosAbiertos: 0, pendientesAbiertos: 4 })]]);
    renderApp('/admin/panel');

    expect(await screen.findByTestId('criticos')).toHaveTextContent('0');
    expect(screen.getByTestId('pendientes')).toHaveTextContent('4');
  });

  it('un empleado no puede entrar', async () => {
    simularApi([sesionDe(EMPLEADO)]);
    renderApp('/admin/panel');

    expect(await screen.findByRole('alert')).toHaveTextContent('No tenés permiso');
  });
});

describe('Listado global', () => {
  it('muestra los reportes con su creador y la paginación', async () => {
    const items = [{ ...DETALLE, descripcion: 'Uno' }, { ...DETALLE, id: 4, descripcion: 'Dos' }];
    simularApi([sesionDe(ADMIN), ['GET', /\/api\/admin\/reportes/, () => respuestaJson(200, { items, page: 1, size: 20, total: 25 })]]);
    renderApp('/admin/reportes');

    expect(await screen.findByRole('link', { name: 'Uno' })).toBeInTheDocument();
    expect(screen.getAllByText(EMPLEADO.email)).toHaveLength(2);
    expect(screen.getByText(/Página 1 de 2/)).toBeInTheDocument();
  });

  it('pide la página siguiente', async () => {
    const fetch = simularApi([
      sesionDe(ADMIN),
      ['GET', /\/api\/admin\/reportes/, () => respuestaJson(200, { items: [DETALLE], page: 1, size: 20, total: 25 })],
    ]);
    renderApp('/admin/reportes');

    await userEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));

    expect(fetch.mock.calls.some(([url]) => url === '/api/admin/reportes?page=2&size=20')).toBe(true);
  });
});

describe('Detalle para el administrador', () => {
  it('muestra la acción sugerida, ambas categorías y el borrador copiable', async () => {
    simularApi([sesionDe(ADMIN), ['GET', '/api/reportes/3', () => respuestaJson(200, DETALLE)]]);
    const portapapeles = vi.fn().mockResolvedValue();
    Object.defineProperty(navigator, 'clipboard', { value: { writeText: portapapeles }, configurable: true });
    renderApp('/reportes/3');

    expect(await screen.findByText('Acción sugerida: Detener actividad')).toBeInTheDocument();
    expect(screen.getByText(/difiere de la automática/)).toBeInTheDocument();
    expect(screen.getByText('Riesgo eléctrico')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Copiar' }));

    expect(portapapeles).toHaveBeenCalledWith(expect.stringContaining('Criticidad Crítica'));
    expect(screen.getByRole('button', { name: 'Copiado' })).toBeInTheDocument();
  });

  it('sin esos datos (empleado) no muestra acción ni borrador', async () => {
    const { accionSugerida, borradorMail, ...delEmpleado } = DETALLE;
    simularApi([sesionDe(EMPLEADO), ['GET', '/api/reportes/3', () => respuestaJson(200, delEmpleado)]]);
    renderApp('/reportes/3');

    expect(await screen.findByRole('heading', { name: 'Reporte #3' })).toBeInTheDocument();
    expect(screen.queryByText(/Acción sugerida/)).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Borrador de mail' })).not.toBeInTheDocument();
  });
});
