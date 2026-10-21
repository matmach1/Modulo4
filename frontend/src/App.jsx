import { NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import RutaProtegida from './auth/RutaProtegida';
import Login from './pages/Login';
import NuevoReporte from './pages/NuevoReporte';
import Registro from './pages/Registro';

function Inicio() {
  const { usuario, cargando } = useAuth();
  if (cargando) return <p>Cargando…</p>;
  if (!usuario) return <Navigate to="/login" replace />;
  return <Navigate to={usuario.rol === 'Administrador' ? '/admin/panel' : '/mis-reportes'} replace />;
}

function Menu() {
  const { usuario } = useAuth();
  if (!usuario) return null;
  return (
    <nav aria-label="Principal">
      {usuario.rol === 'Administrador' && (
        <>
          <NavLink to="/admin/panel">Panel</NavLink>
          <NavLink to="/admin/reportes">Todos los reportes</NavLink>
        </>
      )}
      <NavLink to="/mis-reportes">Mis reportes</NavLink>
      <NavLink to="/reportes/nuevo">Nuevo reporte</NavLink>
      <span className="usuario">{usuario.email}</span>
    </nav>
  );
}

export default function App() {
  return (
    <>
      <header>
        <strong>Reporte de Riesgos Laborales</strong>
        <Menu />
      </header>
      <Routes>
        <Route path="/" element={<Inicio />} />
        <Route path="/login" element={<Login />} />
        <Route path="/registro" element={<Registro />} />
        <Route path="/reportes/nuevo" element={<RutaProtegida><NuevoReporte /></RutaProtegida>} />
        <Route path="*" element={<RutaProtegida><p>Página no encontrada.</p></RutaProtegida>} />
      </Routes>
    </>
  );
}
