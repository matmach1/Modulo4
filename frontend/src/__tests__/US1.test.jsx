import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { EMPLEADO, renderApp, respuestaJson, sesionDe, simularApi } from './utils';

const REPORTE = {
  id: 7,
  descripcion: 'Cable pelado junto al tablero',
  ubicacion: 'Planta 1',
  estado: 'Abierto',
  tipo: 'Pendiente',
  categoria: 'Pendiente',
  criticidad: 'Pendiente',
  categoriaSugerida: null,
  fechaCreacion: '2026-10-21T12:00:00Z',
  creadorEmail: EMPLEADO.email,
};

describe('Registro', () => {
  it('crea la cuenta e inicia sesión', async () => {
    const fetch = simularApi([
      sesionDe(null),
      ['POST', '/api/auth/registro', () => respuestaJson(201, EMPLEADO)],
      ['POST', '/api/auth/login', () => respuestaJson(200, EMPLEADO)],
      ['GET', /\/api\/reportes\/mios/, () => respuestaJson(200, { items: [], page: 1, size: 20, total: 0 })],
    ]);
    renderApp('/registro');

    await userEvent.type(await screen.findByLabelText('Email'), EMPLEADO.email);
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Clave12345');
    await userEvent.click(screen.getByRole('button', { name: 'Registrarme' }));

    expect(await screen.findByRole('heading', { name: 'Mis reportes' })).toBeInTheDocument();
    expect(fetch.mock.calls.some(([url]) => url === '/api/auth/registro')).toBe(true);
  });

  it('muestra el error de la API (email duplicado)', async () => {
    simularApi([
      sesionDe(null),
      ['POST', '/api/auth/registro', () => respuestaJson(409, { title: 'Ya existe una cuenta con ese email.' })],
    ]);
    renderApp('/registro');

    await userEvent.type(await screen.findByLabelText('Email'), EMPLEADO.email);
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Clave12345');
    await userEvent.click(screen.getByRole('button', { name: 'Registrarme' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Ya existe una cuenta con ese email.');
  });
});

describe('Nuevo reporte', () => {
  it('envía descripción, ubicación y categoría sugerida y lleva al detalle', async () => {
    const fetch = simularApi([
      sesionDe(EMPLEADO),
      ['POST', '/api/reportes', () => respuestaJson(201, REPORTE)],
      ['GET', '/api/reportes/7', () => respuestaJson(200, REPORTE)],
    ]);
    renderApp('/reportes/nuevo');

    await userEvent.type(await screen.findByLabelText('Descripción'), REPORTE.descripcion);
    await userEvent.type(screen.getByLabelText('Ubicación'), REPORTE.ubicacion);
    await userEvent.selectOptions(screen.getByLabelText('Categoría sugerida (opcional)'), 'RiesgoElectrico');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar reporte' }));

    expect(await screen.findByRole('heading', { name: 'Reporte #7' })).toBeInTheDocument();
    const [, opciones] = fetch.mock.calls.find(([url, o]) => url === '/api/reportes' && o.method === 'POST');
    expect(JSON.parse(opciones.body)).toEqual({
      descripcion: REPORTE.descripcion,
      ubicacion: REPORTE.ubicacion,
      categoriaSugerida: 'RiesgoElectrico',
    });
    expect(screen.getAllByText('Pendiente de clasificación')).toHaveLength(3);
  });

  it('muestra los errores de validación', async () => {
    simularApi([
      sesionDe(EMPLEADO),
      ['POST', '/api/reportes', () => respuestaJson(400, { title: "El campo 'descripcion' es obligatorio." })],
    ]);
    renderApp('/reportes/nuevo');

    await userEvent.type(await screen.findByLabelText('Descripción'), ' ');
    await userEvent.type(screen.getByLabelText('Ubicación'), 'Planta 1');
    await userEvent.click(screen.getByRole('button', { name: 'Enviar reporte' }));

    expect(await screen.findByRole('alert')).toHaveTextContent("El campo 'descripcion' es obligatorio.");
  });
});

describe('Mis reportes', () => {
  it('lista los reportes con su estado', async () => {
    simularApi([
      sesionDe(EMPLEADO),
      ['GET', /\/api\/reportes\/mios/, () => respuestaJson(200, { items: [REPORTE], page: 1, size: 20, total: 1 })],
    ]);
    renderApp('/mis-reportes');

    expect(await screen.findByRole('link', { name: REPORTE.descripcion })).toHaveAttribute('href', '/reportes/7');
    expect(screen.getByText('Abierto')).toBeInTheDocument();
  });

  it('muestra un estado vacío si no hay reportes (FR-033)', async () => {
    simularApi([
      sesionDe(EMPLEADO),
      ['GET', /\/api\/reportes\/mios/, () => respuestaJson(200, { items: [], page: 1, size: 20, total: 0 })],
    ]);
    renderApp('/mis-reportes');

    expect(await screen.findByText(/Todavía no creaste reportes/)).toBeInTheDocument();
    expect(screen.queryByRole('table')).not.toBeInTheDocument();
  });
});

describe('Detalle', () => {
  it('muestra acceso denegado ante 403', async () => {
    simularApi([
      sesionDe(EMPLEADO),
      ['GET', '/api/reportes/9', () => respuestaJson(403, { title: 'No tenés permiso para ver este reporte.' })],
    ]);
    renderApp('/reportes/9');

    expect(await screen.findByRole('alert')).toHaveTextContent('No tenés permiso para ver esto.');
  });
});
