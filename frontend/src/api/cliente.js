// Cliente HTTP de la API: envía la cookie de sesión y traduce los errores ProblemDetails.

export class ErrorApi extends Error {
  constructor(status, problema) {
    super(problema?.title ?? `Error ${status}`);
    this.status = status;
    this.problema = problema;
  }
}

let alNoAutenticado = null;

// El contexto de sesión se registra acá para enterarse cuando la sesión venció (401).
export function configurarNoAutenticado(callback) {
  alNoAutenticado = callback;
}

export async function api(ruta, { metodo = 'GET', cuerpo, formData } = {}) {
  const opciones = { method: metodo, credentials: 'include', headers: {} };
  if (cuerpo !== undefined) {
    opciones.headers['Content-Type'] = 'application/json';
    opciones.body = JSON.stringify(cuerpo);
  } else if (formData) {
    opciones.body = formData;
  }

  const respuesta = await fetch(ruta, opciones);

  if (respuesta.status === 401 && alNoAutenticado && ruta !== '/api/auth/login') {
    alNoAutenticado();
  }

  if (!respuesta.ok) {
    let problema = null;
    try {
      problema = await respuesta.json();
    } catch {
      // Sin cuerpo JSON: alcanza con el código.
    }
    throw new ErrorApi(respuesta.status, problema);
  }

  if (respuesta.status === 204) {
    return null;
  }

  const tipo = respuesta.headers.get('content-type') ?? '';
  return tipo.includes('json') ? respuesta.json() : respuesta;
}

export function mensajeDeError(error) {
  if (error instanceof ErrorApi) {
    if (error.status === 403) return 'No tenés permiso para ver esto.';
    if (error.status === 404) return 'No existe.';
    return error.problema?.title ?? error.message;
  }
  return 'No se pudo conectar con el servidor.';
}
