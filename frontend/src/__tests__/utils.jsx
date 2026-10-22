import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import { AuthProvider } from '../auth/AuthContext';
import App from '../App';

export function respuestaJson(status, cuerpo) {
  return new Response(cuerpo === undefined ? null : JSON.stringify(cuerpo), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

// Simula la API: recibe una lista de [método, ruta (string o RegExp), respuesta | función].
// Las llamadas quedan registradas en fetch.mock.calls.
export function simularApi(rutas) {
  const fetchSimulado = vi.fn(async (url, opciones = {}) => {
    const metodo = opciones.method ?? 'GET';
    for (const [m, ruta, respuesta] of rutas) {
      const coincide = typeof ruta === 'string' ? url === ruta : ruta.test(url);
      if (m === metodo && coincide) {
        return typeof respuesta === 'function' ? respuesta(url, opciones) : respuesta();
      }
    }
    return respuestaJson(404, { title: `Sin simular: ${metodo} ${url}` });
  });
  vi.stubGlobal('fetch', fetchSimulado);
  return fetchSimulado;
}

export const EMPLEADO = { id: 1, email: 'ana@riesgos.test', rol: 'Empleado' };
export const ADMIN = { id: 2, email: 'admin@riesgos.test', rol: 'Administrador' };

export function sesionDe(usuario) {
  return ['GET', '/api/auth/yo', () => (usuario ? respuestaJson(200, usuario) : respuestaJson(401, { title: 'No autenticado' }))];
}

export function renderApp(ruta) {
  return render(
    <MemoryRouter initialEntries={[ruta]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <AuthProvider>
        <App />
      </AuthProvider>
    </MemoryRouter>,
  );
}
