import { NavLink, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import RutaProtegida from './auth/RutaProtegida';
import CambiarPassword from './pages/CambiarPassword';
import DetalleReporte from './pages/DetalleReporte';
import ListadoAdmin from './pages/ListadoAdmin';
import Login from './pages/Login';
import MisReportes from './pages/MisReportes';
import NuevoReporte from './pages/NuevoReporte';
import Panel from './pages/Panel';
import Registro from './pages/Registro';

function Inicio() {
  const { usuario, cargando } = useAuth();
  if (cargando) return <p>Cargando…</p>;
  if (!usuario) return <Navigate to="/login" replace />;
  return <Navigate to={usuario.rol === 'Administrador' ? '/admin/panel' : '/mis-reportes'} replace />;
}

function Menu() {
  const { usuario, logout } = useAuth();
  const navegar = useNavigate();
  if (!usuario) return null;

  async function salir() {
    await logout();
    navegar('/login');
  }

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
      <NavLink to="/cuenta/password">Cambiar contraseña</NavLink>
      <span className="usuario">{usuario.email}</span>
      <button type="button" className="secundario" onClick={salir}>Cerrar sesión</button>
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
        <Route path="/mis-reportes" element={<RutaProtegida><MisReportes /></RutaProtegida>} />
        <Route path="/reportes/nuevo" element={<RutaProtegida><NuevoReporte /></RutaProtegida>} />
        <Route path="/admin/panel" element={<RutaProtegida rol="Administrador"><Panel /></RutaProtegida>} />
        <Route path="/admin/reportes" element={<RutaProtegida rol="Administrador"><ListadoAdmin /></RutaProtegida>} />
        <Route path="/reportes/:id" element={<RutaProtegida><DetalleReporte /></RutaProtegida>} />
        <Route path="/cuenta/password" element={<RutaProtegida><CambiarPassword /></RutaProtegida>} />
        <Route path="*" element={<RutaProtegida><p>Página no encontrada.</p></RutaProtegida>} />
      </Routes>
    </>
  );
}
