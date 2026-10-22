import { Link } from 'react-router-dom';
import { CATEGORIAS, CRITICIDADES, ESTADOS, fechaHora, texto } from '../textos';

export function EtiquetaCriticidad({ valor }) {
  return <span className={`etiqueta ${valor}`}>{texto(CRITICIDADES, valor)}</span>;
}

export default function TablaReportes({ reportes, mostrarCreador = false }) {
  return (
    <table>
      <thead>
        <tr>
          <th>Fecha</th>
          <th>Descripción</th>
          <th>Ubicación</th>
          <th>Estado</th>
          <th>Categoría</th>
          <th>Criticidad</th>
          {mostrarCreador && <th>Creado por</th>}
        </tr>
      </thead>
      <tbody>
        {reportes.map((r) => (
          <tr key={r.id}>
            <td>{fechaHora(r.fechaCreacion)}</td>
            <td><Link to={`/reportes/${r.id}`}>{r.descripcion}</Link></td>
            <td>{r.ubicacion}</td>
            <td>{texto(ESTADOS, r.estado)}</td>
            <td>{texto(CATEGORIAS, r.categoria)}</td>
            <td><EtiquetaCriticidad valor={r.criticidad} /></td>
            {mostrarCreador && <td>{r.creadorEmail}</td>}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
