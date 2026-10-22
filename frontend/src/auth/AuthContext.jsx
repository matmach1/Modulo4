import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api, configurarNoAutenticado } from '../api/cliente';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(null);
  const [cargando, setCargando] = useState(true);

  useEffect(() => {
    configurarNoAutenticado(() => setUsuario(null));
    api('/api/auth/yo')
      .then(setUsuario)
      .catch(() => setUsuario(null))
      .finally(() => setCargando(false));
  }, []);

  const login = useCallback(async (email, password) => {
    const datos = await api('/api/auth/login', { metodo: 'POST', cuerpo: { email, password } });
    setUsuario(datos);
    return datos;
  }, []);

  const logout = useCallback(async () => {
    try {
      await api('/api/auth/logout', { metodo: 'POST' });
    } catch {
      // Si la sesión ya había vencido o falla la red, igual se da por cerrada en el navegador.
    }
    setUsuario(null);
  }, []);

  const valor = useMemo(() => ({ usuario, cargando, login, logout, setUsuario }), [usuario, cargando, login, logout]);

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
