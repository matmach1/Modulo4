import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, mensajeDeError } from '../api/cliente';

// Contadores solo de reportes en estado Abierto (FR-026); en cero se muestran igual (FR-033).
export default function Panel() {
  const [panel, setPanel] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api('/api/admin/panel')
      .then(setPanel)
      .catch((e) => setError(mensajeDeError(e)));
  }, []);

  return (
    <main>
      <h1>Panel</h1>
      {error && <p role="alert" className="error">{error}</p>}
      {!panel && !error && <p>Cargando…</p>}
      {panel && (
        <div className="contadores">
          <section className="tarjeta contador" aria-labelledby="titulo-criticos">
            <h2 id="titulo-criticos">Críticos abiertos</h2>
            <strong data-testid="criticos">{panel.criticosAbiertos}</strong>
            <p>Reportes con criticidad Crítica en estado Abierto.</p>
          </section>
          <section className="tarjeta contador" aria-labelledby="titulo-pendientes">
            <h2 id="titulo-pendientes">Pendientes de clasificación</h2>
            <strong data-testid="pendientes">{panel.pendientesAbiertos}</strong>
            <p>Reportes en estado Abierto que la IA no pudo clasificar.</p>
          </section>
        </div>
      )}
      <p>
        <Link to="/admin/reportes">Ver todos los reportes</Link>
      </p>
    </main>
  );
}
