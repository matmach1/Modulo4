import { useState } from 'react';
import { api, mensajeDeError } from '../api/cliente';

// Solo el creador, mientras el reporte esté Abierto (FR-012). Editar vuelve a clasificar el reporte.
export default function EditarReporte({ reporte, onGuardado }) {
  const [editando, setEditando] = useState(false);
  const [descripcion, setDescripcion] = useState(reporte.descripcion);
  const [ubicacion, setUbicacion] = useState(reporte.ubicacion);
  const [error, setError] = useState('');
  const [enviando, setEnviando] = useState(false);

  async function guardar(evento) {
    evento.preventDefault();
    setError('');
    setEnviando(true);
    try {
      await api(`/api/reportes/${reporte.id}`, { metodo: 'PUT', cuerpo: { descripcion, ubicacion } });
      setEditando(false);
      await onGuardado();
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  if (!editando) {
    return (
      <button type="button" className="secundario" onClick={() => setEditando(true)}>
        Editar reporte
      </button>
    );
  }

  return (
    <form onSubmit={guardar} aria-label="Editar reporte">
      <label>
        Descripción
        <textarea value={descripcion} onChange={(e) => setDescripcion(e.target.value)} required maxLength={2000} rows={5} />
      </label>
      <label>
        Ubicación
        <input value={ubicacion} onChange={(e) => setUbicacion(e.target.value)} required maxLength={200} />
      </label>
      <p className="vacio">Al guardar, el reporte se vuelve a clasificar.</p>
      {error && <p role="alert" className="error">{error}</p>}
      <div className="paginacion">
        <button type="submit" disabled={enviando}>Guardar cambios</button>
        <button type="button" className="secundario" onClick={() => setEditando(false)}>Cancelar</button>
      </div>
    </form>
  );
}
