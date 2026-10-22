import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { EMPLEADO, renderApp, respuestaJson, sesionDe, simularApi } from './utils';

describe('Cambiar contraseña', () => {
  it('envía la actual y la nueva y confirma el cambio', async () => {
    const fetch = simularApi([sesionDe(EMPLEADO), ['PUT', '/api/auth/password', () => new Response(null, { status: 204 })]]);
    renderApp('/cuenta/password');

    await userEvent.type(await screen.findByLabelText('Contraseña actual'), 'Clave12345');
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaClave2026');
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));

    expect(await screen.findByRole('status')).toHaveTextContent('la contraseña se cambió');
    const [, opciones] = fetch.mock.calls.find(([url]) => url === '/api/auth/password');
    expect(JSON.parse(opciones.body)).toEqual({ passwordActual: 'Clave12345', passwordNueva: 'NuevaClave2026' });
  });

  it('muestra el error si la actual es incorrecta', async () => {
    simularApi([
      sesionDe(EMPLEADO),
      ['PUT', '/api/auth/password', () => respuestaJson(400, { title: 'La contraseña actual no es correcta.' })],
    ]);
    renderApp('/cuenta/password');

    await userEvent.type(await screen.findByLabelText('Contraseña actual'), 'Equivocada1');
    await userEvent.type(screen.getByLabelText('Contraseña nueva'), 'NuevaClave2026');
    await userEvent.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('La contraseña actual no es correcta.');
  });
});

describe('Cerrar sesión (FR-005a)', () => {
  const misReportes = ['GET', /\/api\/reportes\/mios/, () => respuestaJson(200, { items: [], page: 1, size: 20, total: 0 })];

  it('llama al logout del servidor y vuelve al login', async () => {
    const fetch = simularApi([sesionDe(EMPLEADO), misReportes, ['POST', '/api/auth/logout', () => new Response(null, { status: 204 })]]);
    renderApp('/mis-reportes');

    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));

    expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
    expect(fetch.mock.calls.some(([url, o]) => url === '/api/auth/logout' && o.method === 'POST')).toBe(true);
    expect(screen.queryByText(EMPLEADO.email)).not.toBeInTheDocument();
  });

  it('si la sesión ya había vencido, igual vuelve al login', async () => {
    simularApi([sesionDe(EMPLEADO), misReportes, ['POST', '/api/auth/logout', () => respuestaJson(401, { title: 'No autenticado' })]]);
    renderApp('/mis-reportes');

    await userEvent.click(await screen.findByRole('button', { name: 'Cerrar sesión' }));

    expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
  });
});
