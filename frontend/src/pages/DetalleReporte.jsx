import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api, mensajeDeError } from '../api/cliente';
import DetalleAdmin from '../components/DetalleAdmin';
import { EtiquetaCriticidad } from '../components/TablaReportes';
import usePollingClasificacion, { estaPendiente } from '../hooks/usePollingClasificacion';
import { CATEGORIAS, ESTADOS, TIPOS, fechaHora, texto } from '../textos';

export default function DetalleReporte() {
  const { id } = useParams();
  const [reporte, setReporte] = useState(null);
  const [error, setError] = useState('');

  const cargar = useCallback(
    () =>
      api(`/api/reportes/${id}`)
        .then((datos) => {
          setReporte(datos);
          return datos;
        })
        .catch((e) => setError(mensajeDeError(e))),
    [id],
  );

  useEffect(() => {
    cargar();
  }, [cargar]);

  usePollingClasificacion(reporte, cargar);

  if (error) return <main><p role="alert" className="error">{error}</p></main>;
  if (!reporte) return <main><p>Cargando…</p></main>;

  return (
    <main>
      <h1>Reporte #{reporte.id}</h1>
      <DetalleAdmin reporte={reporte} />
      <section className="tarjeta">
        <dl className="datos">
          <dt>Estado</dt>
          <dd>{texto(ESTADOS, reporte.estado)}</dd>
          <dt>Descripción</dt>
          <dd>{reporte.descripcion}</dd>
          <dt>Ubicación</dt>
          <dd>{reporte.ubicacion}</dd>
          <dt>Creado</dt>
          <dd>{fechaHora(reporte.fechaCreacion)} por {reporte.creadorEmail}</dd>
        </dl>
      </section>

      <section className="tarjeta">
        <h2>Clasificación</h2>
        <dl className="datos">
          <dt>Tipo</dt>
          <dd>{texto(TIPOS, reporte.tipo)}</dd>
          <dt>Categoría</dt>
          <dd>{texto(CATEGORIAS, reporte.categoria)}</dd>
          <dt>Criticidad</dt>
          <dd><EtiquetaCriticidad valor={reporte.criticidad} /></dd>
          {reporte.categoriaSugerida && (
            <>
              <dt>
                Categoría sugerida
                {reporte.categoriaSugerida !== reporte.categoria && reporte.categoria !== 'Pendiente' && ' (difiere de la automática)'}
              </dt>
              <dd>{texto(CATEGORIAS, reporte.categoriaSugerida)}</dd>
            </>
          )}
        </dl>
        {estaPendiente(reporte) && (
          <p className="vacio">La clasificación se completa en unos segundos. Si no aparece, queda pendiente.</p>
        )}
      </section>
    </main>
  );
}
