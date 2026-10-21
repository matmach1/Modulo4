import { useState } from 'react';

// Solo se muestra si la API envió estos datos, cosa que hace únicamente para el administrador.
export default function DetalleAdmin({ reporte }) {
  const [copiado, setCopiado] = useState(false);
  const borrador = reporte.borradorMail;

  async function copiar() {
    await navigator.clipboard.writeText(`${borrador.asunto}\n\n${borrador.cuerpo}`);
    setCopiado(true);
  }

  return (
    <>
      {reporte.accionSugerida && (
        <p className="aviso" role="status">
          Acción sugerida: {reporte.accionSugerida}
        </p>
      )}
      {borrador && (
        <section className="tarjeta" aria-labelledby="titulo-borrador">
          <h2 id="titulo-borrador">Borrador de mail</h2>
          <p>
            <strong>Asunto:</strong> {borrador.asunto}
          </p>
          <pre style={{ whiteSpace: 'pre-wrap' }}>{borrador.cuerpo}</pre>
          <button type="button" className="secundario" onClick={copiar}>
            {copiado ? 'Copiado' : 'Copiar'}
          </button>
        </section>
      )}
    </>
  );
}
