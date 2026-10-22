import { useState } from 'react';
import { api, mensajeDeError } from '../api/cliente';
import { fechaHora } from '../textos';

// Acciones y cierre: visibles para ambos roles. Los controles de gestión, solo para el administrador
// (el backend igual rechaza a un empleado con 403).
export default function GestionReporte({ reporte, esAdmin, onCambio }) {
  const [accionRealizada, setAccionRealizada] = useState('');
  const [observacion, setObservacion] = useState('');
  const [nuevaAccion, setNuevaAccion] = useState('');
  const [error, setError] = useState('');
  const [enviando, setEnviando] = useState(false);

  async function ejecutar(operacion) {
    setError('');
    setEnviando(true);
    try {
      await operacion();
      await onCambio();
      return true;
    } catch (e) {
      // 409: otra operación cambió el reporte; se avisa y se recarga el estado actual.
      setError(e.status === 409 ? `${mensajeDeError(e)} Se recargó el reporte.` : mensajeDeError(e));
      if (e.status === 409) await onCambio();
      return false;
    } finally {
      setEnviando(false);
    }
  }

  const cambiarEstado = (cuerpo) => api(`/api/admin/reportes/${reporte.id}/estado`, { metodo: 'POST', cuerpo });

  async function cerrar(evento) {
    evento.preventDefault();
    await ejecutar(() => cambiarEstado({ estado: 'Cerrado', accionRealizada, observacionCierre: observacion }));
  }

  async function registrarAccion(evento) {
    evento.preventDefault();
    const ok = await ejecutar(() =>
      api(`/api/admin/reportes/${reporte.id}/acciones`, { metodo: 'POST', cuerpo: { descripcion: nuevaAccion } }),
    );
    if (ok) setNuevaAccion('');
  }

  return (
    <>
      <section className="tarjeta" aria-labelledby="titulo-acciones">
        <h2 id="titulo-acciones">Acciones correctivas</h2>
        {reporte.acciones.length === 0 ? (
          <p className="vacio">Sin acciones registradas</p>
        ) : (
          <ul>
            {reporte.acciones.map((a) => (
              <li key={a.id}>
                {a.descripcion} — {fechaHora(a.fecha)} por {a.responsableEmail}
              </li>
            ))}
          </ul>
        )}
      </section>

      {(reporte.inicioProgreso || reporte.cierre) && (
        <section className="tarjeta" aria-labelledby="titulo-seguimiento">
          <h2 id="titulo-seguimiento">Seguimiento</h2>
          <dl className="datos">
            {reporte.inicioProgreso && (
              <>
                <dt>En progreso desde</dt>
                <dd>{fechaHora(reporte.inicioProgreso.fecha)} por {reporte.inicioProgreso.usuarioEmail}</dd>
              </>
            )}
            {reporte.cierre && (
              <>
                <dt>Cerrado</dt>
                <dd>{fechaHora(reporte.cierre.fecha)} por {reporte.cierre.usuarioEmail}</dd>
                <dt>Acción realizada</dt>
                <dd>{reporte.cierre.accionRealizada}</dd>
                <dt>Observación de cierre</dt>
                <dd>{reporte.cierre.observacionCierre}</dd>
              </>
            )}
          </dl>
        </section>
      )}

      {esAdmin && reporte.estado !== 'Cerrado' && (
        <section className="tarjeta" aria-labelledby="titulo-gestion">
          <h2 id="titulo-gestion">Gestión</h2>
          {error && <p role="alert" className="error">{error}</p>}

          {reporte.estado === 'Abierto' && (
            <button type="button" disabled={enviando} onClick={() => ejecutar(() => cambiarEstado({ estado: 'EnProgreso' }))}>
              Pasar a En Progreso
            </button>
          )}

          {reporte.estado === 'EnProgreso' && (
            <form onSubmit={cerrar} aria-label="Cerrar reporte">
              <label>
                Acción realizada
                <textarea value={accionRealizada} onChange={(e) => setAccionRealizada(e.target.value)} required maxLength={1000} />
              </label>
              <label>
                Observación de cierre
                <textarea value={observacion} onChange={(e) => setObservacion(e.target.value)} required maxLength={1000} />
              </label>
              <button type="submit" disabled={enviando}>Cerrar reporte</button>
            </form>
          )}

          <form onSubmit={registrarAccion} aria-label="Nueva acción correctiva">
            <label>
              Nueva acción correctiva o preventiva
              <textarea value={nuevaAccion} onChange={(e) => setNuevaAccion(e.target.value)} required maxLength={1000} />
            </label>
            <button type="submit" className="secundario" disabled={enviando}>Registrar acción</button>
          </form>
        </section>
      )}
    </>
  );
}
