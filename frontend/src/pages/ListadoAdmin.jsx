import { useEffect, useState } from 'react';
import { api, mensajeDeError } from '../api/cliente';
import Paginacion from '../components/Paginacion';
import TablaReportes from '../components/TablaReportes';

export default function ListadoAdmin() {
  const [page, setPage] = useState(1);
  const [pagina, setPagina] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api(`/api/admin/reportes?page=${page}&size=20`)
      .then(setPagina)
      .catch((e) => setError(mensajeDeError(e)));
  }, [page]);

  return (
    <main>
      <h1>Todos los reportes</h1>
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
