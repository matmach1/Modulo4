import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { ADMIN, EMPLEADO, renderApp, respuestaJson, sesionDe, simularApi } from './utils';

const base = {
  id: 5,
  descripcion: 'Baranda rota',
  ubicacion: 'Escalera norte',
  tipo: 'CondicionInsegura',
  categoria: 'ProblemaInfraestructura',
  criticidad: 'Media',
  categoriaSugerida: null,
  fechaCreacion: '2026-10-21T12:00:00Z',
  creadorEmail: EMPLEADO.email,
  inicioProgreso: null,
  cierre: null,
  acciones: [],
  imagenes: [],
};

function detalleSecuencial(...versiones) {
  let i = 0;
  return () => respuestaJson(200, versiones[Math.min(i++, versiones.length - 1)]);
}

describe('Gestión del reporte', () => {
  it('en Abierto el administrador puede pasarlo a En Progreso', async () => {
    const enProgreso = { ...base, estado: 'EnProgreso', inicioProgreso: { fecha: '2026-10-21T13:00:00Z', usuarioEmail: ADMIN.email } };
    const fetch = simularApi([
      sesionDe(ADMIN),
      ['GET', '/api/reportes/5', detalleSecuencial({ ...base, estado: 'Abierto' }, enProgreso)],
      ['POST', '/api/admin/reportes/5/estado', () => respuestaJson(200, enProgreso)],
    ]);
    renderApp('/reportes/5');

    await userEvent.click(await screen.findByRole('button', { name: 'Pasar a En Progreso' }));

    expect(await screen.findByText('En progreso desde')).toBeInTheDocument();
    const [, opciones] = fetch.mock.calls.find(([url]) => url === '/api/admin/reportes/5/estado');
    expect(JSON.parse(opciones.body)).toEqual({ estado: 'EnProgreso' });
    expect(screen.queryByRole('button', { name: 'Pasar a En Progreso' })).not.toBeInTheDocument();
  });

  it('en En Progreso el administrador cierra con acción realizada y observación', async () => {
    const fetch = simularApi([
      sesionDe(ADMIN),
      ['GET', '/api/reportes/5', () => respuestaJson(200, { ...base, estado: 'EnProgreso' })],
      ['POST', '/api/admin/reportes/5/estado', () => respuestaJson(200, { ...base, estado: 'Cerrado' })],
    ]);
    renderApp('/reportes/5');

    await userEvent.type(await screen.findByLabelText('Acción realizada'), 'Se soldó la baranda');
    await userEvent.type(screen.getByLabelText('Observación de cierre'), 'Verificado');
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar reporte' }));

    const [, opciones] = fetch.mock.calls.find(([url]) => url === '/api/admin/reportes/5/estado');
    expect(JSON.parse(opciones.body)).toEqual({ estado: 'Cerrado', accionRealizada: 'Se soldó la baranda', observacionCierre: 'Verificado' });
  });

  it('registra una acción correctiva y la muestra', async () => {
    const conAccion = { ...base, estado: 'Abierto', acciones: [{ id: 1, descripcion: 'Se señalizó', fecha: '2026-10-21T13:00:00Z', responsableEmail: ADMIN.email }] };
    simularApi([
      sesionDe(ADMIN),
      ['GET', '/api/reportes/5', detalleSecuencial({ ...base, estado: 'Abierto' }, conAccion)],
      ['POST', '/api/admin/reportes/5/acciones', () => respuestaJson(201, conAccion.acciones[0])],
    ]);
    renderApp('/reportes/5');

    expect(await screen.findByText('Sin acciones registradas')).toBeInTheDocument();
    await userEvent.type(screen.getByLabelText('Nueva acción correctiva o preventiva'), 'Se señalizó');
    await userEvent.click(screen.getByRole('button', { name: 'Registrar acción' }));

    expect(await screen.findByText(/Se señalizó —/)).toBeInTheDocument();
  });

  it('ante un conflicto (409) avisa y recarga', async () => {
    const fetch = simularApi([
      sesionDe(ADMIN),
      ['GET', '/api/reportes/5', () => respuestaJson(200, { ...base, estado: 'Abierto' })],
      ['POST', '/api/admin/reportes/5/estado', () => respuestaJson(409, { title: 'El reporte fue modificado por otra operación.' })],
    ]);
    renderApp('/reportes/5');

    await userEvent.click(await screen.findByRole('button', { name: 'Pasar a En Progreso' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Se recargó el reporte.');
    expect(fetch.mock.calls.filter(([url]) => url === '/api/reportes/5')).toHaveLength(2);
  });

  it('un reporte Cerrado muestra el cierre y no ofrece gestión', async () => {
    const cerrado = {
      ...base,
      estado: 'Cerrado',
      cierre: { fecha: '2026-10-21T15:00:00Z', usuarioEmail: ADMIN.email, accionRealizada: 'Se soldó', observacionCierre: 'Ok' },
    };
    simularApi([sesionDe(ADMIN), ['GET', '/api/reportes/5', () => respuestaJson(200, cerrado)]]);
    renderApp('/reportes/5');

    expect(await screen.findByText('Se soldó')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Gestión' })).not.toBeInTheDocument();
  });

  it('el empleado ve acciones y cierre pero no los controles', async () => {
    simularApi([sesionDe(EMPLEADO), ['GET', '/api/reportes/5', () => respuestaJson(200, { ...base, estado: 'EnProgreso' })]]);
    renderApp('/reportes/5');

    expect(await screen.findByText('Sin acciones registradas')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Gestión' })).not.toBeInTheDocument();
  });
});
