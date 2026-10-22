import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, mensajeDeError } from '../api/cliente';
import { CATEGORIAS } from '../textos';

const OPCIONES_CATEGORIA = Object.entries(CATEGORIAS).filter(([valor]) => valor !== 'Pendiente');

export default function NuevoReporte() {
  const navegar = useNavigate();
  const [descripcion, setDescripcion] = useState('');
  const [ubicacion, setUbicacion] = useState('');
  const [categoriaSugerida, setCategoriaSugerida] = useState('');
  const [error, setError] = useState('');
  const [enviando, setEnviando] = useState(false);

  async function enviar(evento) {
    evento.preventDefault();
    setError('');
    setEnviando(true);
    try {
      const reporte = await api('/api/reportes', {
        metodo: 'POST',
        cuerpo: { descripcion, ubicacion, categoriaSugerida: categoriaSugerida || null },
      });
      navegar(`/reportes/${reporte.id}`);
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <main className="tarjeta">
      <h1>Nuevo reporte</h1>
      <form onSubmit={enviar}>
        <label>
          Descripción
          <textarea value={descripcion} onChange={(e) => setDescripcion(e.target.value)} required maxLength={2000} rows={5} />
        </label>
        <label>
          Ubicación
          <input value={ubicacion} onChange={(e) => setUbicacion(e.target.value)} required maxLength={200} />
        </label>
        <label>
          Categoría sugerida (opcional)
          <select value={categoriaSugerida} onChange={(e) => setCategoriaSugerida(e.target.value)}>
            <option value="">Sin sugerencia</option>
            {OPCIONES_CATEGORIA.map(([valor, nombre]) => (
              <option key={valor} value={valor}>{nombre}</option>
            ))}
          </select>
        </label>
        {error && <p role="alert" className="error">{error}</p>}
        <button type="submit" disabled={enviando}>Enviar reporte</button>
      </form>
    </main>
  );
}
