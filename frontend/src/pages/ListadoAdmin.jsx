import { useEffect, useState } from 'react';
import { api, mensajeDeError } from '../api/cliente';
import Paginacion from '../components/Paginacion';
import TablaReportes from '../components/TablaReportes';

export default function ListadoAdmin() {
  const [page, setPage] = useState(1);
  const [orden, setOrden] = useState('fecha');
  const [pagina, setPagina] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api(`/api/admin/reportes?page=${page}&size=20&orden=${orden}`)
      .then(setPagina)
      .catch((e) => setError(mensajeDeError(e)));
  }, [page, orden]);

  function cambiarOrden(evento) {
    setOrden(evento.target.value);
    setPage(1);
  }

  return (
    <main>
      <h1>Todos los reportes</h1>
      <label className="angosta">
        Ordenar por
        <select value={orden} onChange={cambiarOrden}>
          <option value="fecha">Más recientes primero</option>
          <option value="criticidad">Criticidad (Crítica primero)</option>
        </select>
      </label>
      {error && <p role="alert" className="error">{error}</p>}
      {!pagina && !error && <p>Cargando…</p>}
      {pagina && pagina.total === 0 && <p className="vacio">Todavía no hay reportes.</p>}
      {pagina && pagina.total > 0 && (
        <>
          <TablaReportes reportes={pagina.items} mostrarCreador />
          <Paginacion page={pagina.page} size={pagina.size} total={pagina.total} onCambiar={setPage} />
        </>
      )}
    </main>
  );
}
