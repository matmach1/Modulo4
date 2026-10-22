import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { ADMIN, EMPLEADO, renderApp, respuestaJson, sesionDe, simularApi } from './utils';

const base = {
  id: 8,
  descripcion: 'Piso mojado',
  ubicacion: 'Comedor',
  estado: 'Abierto',
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

const imagen = (id) => ({ id, tipoContenido: 'image/jpeg', tamanoBytes: 1000, fechaCarga: '2026-10-21T12:00:00Z', url: `/api/reportes/8/imagenes/${id}` });

describe('Edición', () => {
  it('el creador edita un reporte Abierto y se guarda', async () => {
    const editado = { ...base, descripcion: 'Piso mojado y sin cartel', criticidad: 'Pendiente' };
    let llamadas = 0;
    const fetch = simularApi([
      sesionDe(EMPLEADO),
      ['GET', '/api/reportes/8', () => respuestaJson(200, llamadas++ === 0 ? base : editado)],
      ['PUT', '/api/reportes/8', () => respuestaJson(200, editado)],
    ]);
    renderApp('/reportes/8');

    await userEvent.click(await screen.findByRole('button', { name: 'Editar reporte' }));
    const descripcion = screen.getByLabelText('Descripción');
    await userEvent.clear(descripcion);
    await userEvent.type(descripcion, 'Piso mojado y sin cartel');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar cambios' }));

    expect(await screen.findByText('Piso mojado y sin cartel')).toBeInTheDocument();
    const [, opciones] = fetch.mock.calls.find(([url, o]) => url === '/api/reportes/8' && o.method === 'PUT');
    expect(JSON.parse(opciones.body)).toEqual({ descripcion: 'Piso mojado y sin cartel', ubicacion: 'Comedor' });
  });

  it('no ofrece editar ni adjuntar fuera de Abierto', async () => {
    simularApi([sesionDe(EMPLEADO), ['GET', '/api/reportes/8', () => respuestaJson(200, { ...base, estado: 'EnProgreso' })]]);
    renderApp('/reportes/8');

    expect(await screen.findByRole('heading', { name: 'Reporte #8' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Editar reporte' })).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/Adjuntar imagen/)).not.toBeInTheDocument();
  });

  it('el administrador no edita reportes que no creó', async () => {
    simularApi([sesionDe(ADMIN), ['GET', '/api/reportes/8', () => respuestaJson(200, base)]]);
    renderApp('/reportes/8');

    expect(await screen.findByRole('heading', { name: 'Reporte #8' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Editar reporte' })).not.toBeInTheDocument();
  });
});

describe('Imágenes', () => {
  it('sin imágenes muestra el estado vacío y cuántas quedan', async () => {
    simularApi([sesionDe(EMPLEADO), ['GET', '/api/reportes/8', () => respuestaJson(200, base)]]);
    renderApp('/reportes/8');

    expect(await screen.findByText('Sin imágenes')).toBeInTheDocument();
    expect(screen.getByLabelText(/quedan 5 de 5/)).toBeInTheDocument();
  });

  it('sube una imagen en el campo archivo y la muestra', async () => {
    const conImagen = { ...base, imagenes: [imagen(1)] };
    let llamadas = 0;
    const fetch = simularApi([
      sesionDe(EMPLEADO),
      ['GET', '/api/reportes/8', () => respuestaJson(200, llamadas++ === 0 ? base : conImagen)],
      ['POST', '/api/reportes/8/imagenes', () => respuestaJson(201, imagen(1))],
    ]);
    renderApp('/reportes/8');

    const archivo = new File([new Uint8Array([0xff, 0xd8, 0xff])], 'foto.jpg', { type: 'image/jpeg' });
    await userEvent.upload(await screen.findByLabelText(/Adjuntar imagen/), archivo);

    expect(await screen.findByRole('img', { name: 'Imagen 1 del reporte 8' })).toHaveAttribute('src', '/api/reportes/8/imagenes/1');
    const [, opciones] = fetch.mock.calls.find(([url]) => url === '/api/reportes/8/imagenes');
    expect(opciones.body.get('archivo').name).toBe('foto.jpg');
  });

  it('muestra el error de validación de la API', async () => {
    simularApi([
      sesionDe(EMPLEADO),
      ['GET', '/api/reportes/8', () => respuestaJson(200, base)],
      ['POST', '/api/reportes/8/imagenes', () => respuestaJson(400, { title: 'La imagen supera el máximo de 2 MB.' })],
    ]);
    renderApp('/reportes/8');

    const archivo = new File([new Uint8Array([0xff, 0xd8, 0xff])], 'grande.jpg', { type: 'image/jpeg' });
    await userEvent.upload(await screen.findByLabelText(/Adjuntar imagen/), archivo);

    expect(await screen.findByRole('alert')).toHaveTextContent('La imagen supera el máximo de 2 MB.');
  });

  it('con 5 imágenes ya no ofrece adjuntar', async () => {
    const completo = { ...base, imagenes: [1, 2, 3, 4, 5].map(imagen) };
    simularApi([sesionDe(EMPLEADO), ['GET', '/api/reportes/8', () => respuestaJson(200, completo)]]);
    renderApp('/reportes/8');

    expect(await screen.findByText(/Ya se adjuntaron las 5/)).toBeInTheDocument();
    expect(screen.queryByLabelText(/Adjuntar imagen/)).not.toBeInTheDocument();
  });
});
