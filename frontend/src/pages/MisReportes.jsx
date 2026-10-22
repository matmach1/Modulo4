import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, mensajeDeError } from '../api/cliente';
import Paginacion from '../components/Paginacion';
import TablaReportes from '../components/TablaReportes';

export default function MisReportes() {
  const [page, setPage] = useState(1);
  const [pagina, setPagina] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api(`/api/reportes/mios?page=${page}&size=20`)
      .then(setPagina)
      .catch((e) => setError(mensajeDeError(e)));
  }, [page]);

  return (
    <main>
      <h1>Mis reportes</h1>
      {error && <p role="alert" className="error">{error}</p>}
      {!pagina && !error && <p>Cargando…</p>}
      {pagina && pagina.total === 0 && (
        <p className="vacio">
          Todavía no creaste reportes. <Link to="/reportes/nuevo">Crear el primero</Link>
        </p>
      )}
      {pagina && pagina.total > 0 && (
        <>
          <TablaReportes reportes={pagina.items} />
          <Paginacion page={pagina.page} size={pagina.size} total={pagina.total} onCambiar={setPage} />
        </>
      )}
    </main>
  );
}
