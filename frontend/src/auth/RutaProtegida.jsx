import { Navigate } from 'react-router-dom';
import { useAuth } from './AuthContext';

// Solo controla la navegación; los permisos reales los verifica el backend (Principio III).
export default function RutaProtegida({ rol, children }) {
  const { usuario, cargando } = useAuth();

  if (cargando) return <p>Cargando…</p>;
  if (!usuario) return <Navigate to="/login" replace />;
  if (rol && usuario.rol !== rol) return <p role="alert">No tenés permiso para ver esta página.</p>;

  return children;
}
