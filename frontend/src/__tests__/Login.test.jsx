import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { EMPLEADO, renderApp, respuestaJson, sesionDe, simularApi } from './utils';

describe('Login', () => {
  it('envía las credenciales y entra a la app', async () => {
    const fetch = simularApi([
      sesionDe(null),
      ['POST', '/api/auth/login', () => respuestaJson(200, EMPLEADO)],
      ['GET', /\/api\/reportes\/mios/, () => respuestaJson(200, { items: [], page: 1, size: 20, total: 0 })],
    ]);
    renderApp('/login');

    await userEvent.type(await screen.findByLabelText('Email'), 'ana@riesgos.test');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Clave12345');
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));

    const llamada = fetch.mock.calls.find(([url]) => url === '/api/auth/login');
    expect(JSON.parse(llamada[1].body)).toEqual({ email: 'ana@riesgos.test', password: 'Clave12345' });
    expect(await screen.findByText('ana@riesgos.test')).toBeInTheDocument();
  });

  it('muestra un error genérico ante credenciales inválidas', async () => {
    simularApi([
      sesionDe(null),
      ['POST', '/api/auth/login', () => respuestaJson(401, { title: 'Email o contraseña incorrectos.' })],
    ]);
    renderApp('/login');

    await userEvent.type(await screen.findByLabelText('Email'), 'ana@riesgos.test');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'mala-clave');
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Email o contraseña incorrectos.');
  });

  it('sin sesión, una página protegida redirige al login', async () => {
    simularApi([sesionDe(null)]);
    renderApp('/mis-reportes');

    expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeInTheDocument();
  });
});
