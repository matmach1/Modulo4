export default function Paginacion({ page, size, total, onCambiar }) {
  const paginas = Math.max(1, Math.ceil(total / size));
  return (
    <div className="paginacion">
      <button type="button" className="secundario" disabled={page <= 1} onClick={() => onCambiar(page - 1)}>
        Anterior
      </button>
      <span>
        Página {page} de {paginas} · {total} reportes
      </span>
      <button type="button" className="secundario" disabled={page >= paginas} onClick={() => onCambiar(page + 1)}>
        Siguiente
      </button>
    </div>
  );
}
