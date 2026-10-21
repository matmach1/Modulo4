import { useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { api, mensajeDeError } from '../api/cliente';
import { useAuth } from '../auth/AuthContext';

export default function Registro() {
  const { usuario, login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [enviando, setEnviando] = useState(false);

  if (usuario) return <Navigate to="/" replace />;

  async function enviar(evento) {
    evento.preventDefault();
    setError('');
    setEnviando(true);
    try {
      await api('/api/auth/registro', { metodo: 'POST', cuerpo: { email, password } });
      await login(email, password);
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <main className="tarjeta angosta">
      <h1>Crear cuenta</h1>
      <form onSubmit={enviar}>
        <label>
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
        </label>
        <label>
          Contraseña
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
            autoComplete="new-password"
            aria-describedby="ayuda-password"
          />
        </label>
        <small id="ayuda-password">Al menos 8 caracteres.</small>
        {error && <p role="alert" className="error">{error}</p>}
        <button type="submit" disabled={enviando}>Registrarme</button>
      </form>
      <p>
        ¿Ya tenés cuenta? <Link to="/login">Iniciá sesión</Link>
      </p>
    </main>
  );
}
