import { useState } from 'react';
import { api, mensajeDeError } from '../api/cliente';

const MAXIMO = 5;

// Galería para quien puede ver el reporte; la subida, solo para el creador en Abierto (FR-013).
export default function Imagenes({ reporte, puedeAdjuntar, onSubida }) {
  const [error, setError] = useState('');
  const [enviando, setEnviando] = useState(false);
  const restantes = MAXIMO - reporte.imagenes.length;

  async function subir(evento) {
    const archivo = evento.target.files[0];
    evento.target.value = '';
    if (!archivo) return;
    setError('');
    setEnviando(true);
    try {
      const formData = new FormData();
      formData.append('archivo', archivo);
      await api(`/api/reportes/${reporte.id}/imagenes`, { metodo: 'POST', formData });
      await onSubida();
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <section className="tarjeta" aria-labelledby="titulo-imagenes">
      <h2 id="titulo-imagenes">Imágenes</h2>
      {reporte.imagenes.length === 0 ? (
        <p className="vacio">Sin imágenes</p>
      ) : (
        <div className="galeria">
          {reporte.imagenes.map((imagen, i) => (
            <a key={imagen.id} href={imagen.url} target="_blank" rel="noreferrer">
              <img src={imagen.url} alt={`Imagen ${i + 1} del reporte ${reporte.id}`} />
            </a>
          ))}
        </div>
      )}
      {puedeAdjuntar && restantes > 0 && (
        <label>
          Adjuntar imagen (JPG o PNG, hasta 2 MB) — quedan {restantes} de {MAXIMO}
          <input type="file" accept="image/jpeg,image/png" onChange={subir} disabled={enviando} />
        </label>
      )}
      {puedeAdjuntar && restantes === 0 && <p className="vacio">Ya se adjuntaron las {MAXIMO} imágenes permitidas.</p>}
      {error && <p role="alert" className="error">{error}</p>}
    </section>
  );
}
