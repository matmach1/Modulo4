import { useState } from 'react';
import { api, mensajeDeError } from '../api/cliente';

export default function CambiarPassword() {
  const [actual, setActual] = useState('');
  const [nueva, setNueva] = useState('');
  const [error, setError] = useState('');
  const [listo, setListo] = useState(false);
  const [enviando, setEnviando] = useState(false);

  async function enviar(evento) {
    evento.preventDefault();
    setError('');
    setListo(false);
    setEnviando(true);
    try {
      await api('/api/auth/password', { metodo: 'PUT', cuerpo: { passwordActual: actual, passwordNueva: nueva } });
      setListo(true);
      setActual('');
      setNueva('');
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <main className="tarjeta angosta">
      <h1>Cambiar contraseña</h1>
      <form onSubmit={enviar}>
        <label>
          Contraseña actual
          <input type="password" value={actual} onChange={(e) => setActual(e.target.value)} required autoComplete="current-password" />
        </label>
        <label>
          Contraseña nueva
          <input
            type="password"
            value={nueva}
            onChange={(e) => setNueva(e.target.value)}
            required
            minLength={8}
            autoComplete="new-password"
          />
        </label>
        <small>Al menos 8 caracteres. Las demás sesiones abiertas se cierran.</small>
        {error && <p role="alert" className="error">{error}</p>}
        {listo && <p role="status">Listo: la contraseña se cambió.</p>}
        <button type="submit" disabled={enviando}>Cambiar contraseña</button>
      </form>
    </main>
  );
}
