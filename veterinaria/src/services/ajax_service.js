import { obtener_api_url } from '@/config/debug';
import { token_service } from './token_service';
import { vibrar_error } from './vibracion_service';
import { guardar_copia, obtener_copia } from './copia_local_service';

const TIEMPO_ESPERA_DEFECTO_MS = 8000;

let promesaRenovacion = null;
const suscriptoresExpiracion = new Set();

/**
 * Registra un callback para ser notificado cuando la sesión expire definitivamente.
 * @param {Function} callback
 * @returns {Function} Función para desuscribirse.
 */
export function al_expirar_sesion(callback) {
  suscriptoresExpiracion.add(callback);
  return () => suscriptoresExpiracion.delete(callback);
}

function notificar_sesion_expirada() {
  suscriptoresExpiracion.forEach((cb) => {
    try {
      cb();
    } catch (e) {
      console.error('Error en suscriptor de expiración de sesión:', e);
    }
  });
}

/**
 * Ejecuta una llamada atómica para renovar el par de tokens.
 * @param {string} baseUrl
 * @param {string} refreshToken
 * @returns {Promise<string>} Nuevo access token
 */
async function renovar_tokens_servidor(baseUrl, refreshToken) {
  const urlRefresh = `${baseUrl}/api/sesion/refresh`;
  const respuesta = await fetch(urlRefresh, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
    body: JSON.stringify({ refresh_token: refreshToken }),
  });

  if (!respuesta.ok) {
    let detalleError = 'No se pudo renovar la sesión.';
    try {
      const errJson = await respuesta.json();
      if (errJson?.mensaje) detalleError = errJson.mensaje;
    } catch (_e) {
      // Ignorar si el cuerpo no es JSON
    }
    const error = new Error(detalleError);
    error.status = respuesta.status;
    throw error;
  }

  const datos = await respuesta.json();
  await token_service.guardar_tokens(datos.token, datos.refresh_token);
  return datos.token;
}

/**
 * Normaliza y ejecuta peticiones HTTP usando fetch con control de timeout, inyección de token,
 * soporte de FormData multipart, renovación automática ante 401 y soporte de copia offline guardable.
 *
 * @param {string} ruta - Endpoint relativo (ej: '/api/productos') o URL absoluta.
 * @param {RequestInit & { timeoutMs?: number, guardable?: boolean }} opciones - Opciones de fetch y configuración adicional.
 * @param {boolean} esReintento - Bandera interna para evitar bucles de renovación.
 * @returns {Promise<any>} Datos parseados de la respuesta.
 */
export async function peticion_ajax(ruta, opciones = {}, esReintento = false) {
  const baseUrl = obtener_api_url();
  const urlCompleta = ruta.startsWith('http://') || ruta.startsWith('https://')
    ? ruta
    : `${baseUrl}${ruta.startsWith('/') ? '' : '/'}${ruta}`;

  const timeoutMs = opciones.timeoutMs || TIEMPO_ESPERA_DEFECTO_MS;
  const guardable = opciones.guardable === true;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  const tokenActual = token_service.obtener_token();
  const esFormData = typeof FormData !== 'undefined' && opciones.body instanceof FormData;

  const cabeceras = {
    'Accept': 'application/json',
    ...(opciones.body && !esFormData ? { 'Content-Type': 'application/json' } : {}),
    ...(tokenActual && !opciones.headers?.Authorization && !opciones.headers?.authorization
      ? { 'Authorization': `Bearer ${tokenActual}` }
      : {}),
    ...opciones.headers,
  };

  try {
    const respuesta = await fetch(urlCompleta, {
      ...opciones,
      headers: cabeceras,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    // Manejo de expiración de token (401)
    if (respuesta.status === 401 && !esReintento) {
      const esEndpointSesion = ruta.includes('/api/sesion/login') || ruta.includes('/api/sesion/refresh');
      const refreshToken = token_service.obtener_refresh_token();

      if (!esEndpointSesion && refreshToken) {
        if (!promesaRenovacion) {
          promesaRenovacion = renovar_tokens_servidor(baseUrl, refreshToken);
        }

        try {
          const nuevoToken = await promesaRenovacion;
          const nuevasOpciones = {
            ...opciones,
            headers: {
              ...opciones.headers,
              'Authorization': `Bearer ${nuevoToken}`,
            },
          };
          return await peticion_ajax(ruta, nuevasOpciones, true);
        } catch (errorRenovacion) {
          await token_service.limpiar_tokens();
          notificar_sesion_expirada();
          vibrar_error();
          throw errorRenovacion;
        } finally {
          promesaRenovacion = null;
        }
      }
    }

    if (!respuesta.ok) {
      vibrar_error();
      let detalleError = `Error HTTP ${respuesta.status}: ${respuesta.statusText}`;
      let errorJson = null;
      try {
        errorJson = await respuesta.json();
        if (errorJson?.mensaje) {
          detalleError = errorJson.mensaje;
        } else if (errorJson?.title) {
          detalleError = errorJson.title;
        }
      } catch {
        // En caso de que el cuerpo del error no sea JSON
      }
      const err = new Error(detalleError);
      err.status = respuesta.status;
      err.codigo = errorJson?.codigo;
      err.mensaje = errorJson?.mensaje || detalleError;
      throw err;
    }

    const contentType = respuesta.headers.get('content-type');
    const datosRespuesta = contentType && contentType.includes('application/json')
      ? await respuesta.json()
      : await respuesta.text();

    if (guardable) {
      guardar_copia(ruta, datosRespuesta);
    }

    return datosRespuesta;
  } catch (error) {
    clearTimeout(timeoutId);

    // Si es falla de red y la petición es guardable, intentar resolver con copia local sin error
    const esFallaRed = error.name === 'AbortError' ||
                       error.name === 'TypeError' ||
                       error.status === 0 ||
                       error.message?.includes('Failed to fetch') ||
                       error.message?.includes('NetworkError') ||
                       error.message?.includes('conexión');

    if (guardable && esFallaRed) {
      const copia = obtener_copia(ruta);
      if (copia !== null && copia !== undefined) {
        return copia;
      }
    }

    vibrar_error();

    if (error.name === 'AbortError') {
      const errTimeout = new Error(`Tiempo de espera agotado (${timeoutMs / 1000}s) al contactar al servidor.`);
      errTimeout.status = 0;
      throw errTimeout;
    }

    if (error.message?.includes('Failed to fetch') || error.name === 'TypeError') {
      const errRed = new Error(`No se pudo establecer conexión con el servidor (${baseUrl}). Verifique la red y que la API esté activa.`);
      errRed.status = 0;
      throw errRed;
    }

    throw error;
  }
}

/**
 * Realiza una petición GET autenticada que retorna un Blob binario (para PDFs, imágenes, etc.).
 * @param {string} endpoint - Ruta relativa o absoluta del recurso binario.
 * @returns {Promise<Blob>}
 */
export async function ajax_binario(endpoint) {
  const baseUrl = obtener_api_url();
  const urlCompleta = endpoint.startsWith('http://') || endpoint.startsWith('https://')
    ? endpoint
    : `${baseUrl}${endpoint.startsWith('/') ? '' : '/'}${endpoint}`;

  const tokenActual = token_service.obtener_token();
  const cabeceras = {
    ...(tokenActual ? { 'Authorization': `Bearer ${tokenActual}` } : {}),
  };

  try {
    const respuesta = await fetch(urlCompleta, {
      method: 'GET',
      headers: cabeceras,
    });

    if (!respuesta.ok) {
      vibrar_error();
      let detalleError = `Error HTTP ${respuesta.status}: ${respuesta.statusText}`;
      try {
        const errorJson = await respuesta.json();
        if (errorJson?.mensaje) detalleError = errorJson.mensaje;
      } catch {
        // No es JSON
      }
      const err = new Error(detalleError);
      err.status = respuesta.status;
      throw err;
    }

    return await respuesta.blob();
  } catch (error) {
    vibrar_error();
    throw error;
  }
}

export const ajax_service = {
  get: (ruta, opciones = {}) => peticion_ajax(ruta, { ...opciones, method: 'GET' }),
  post: (ruta, datos, opciones = {}) => {
    const esFormData = typeof FormData !== 'undefined' && datos instanceof FormData;
    return peticion_ajax(ruta, {
      ...opciones,
      method: 'POST',
      body: esFormData ? datos : JSON.stringify(datos),
    });
  },
  binario: ajax_binario,
};

/**
 * Función genérica de envío con soporte automático para JSON y FormData (multipart).
 * @param {string} endpoint
 * @param {string} metodo
 * @param {any} datos
 * @param {object} opcionesExtra
 */
export async function enviar(endpoint, metodo = 'GET', datos = null, opcionesExtra = {}) {
  const opciones = { method: metodo, ...opcionesExtra };
  if (datos && (metodo === 'POST' || metodo === 'PUT' || metodo === 'PATCH')) {
    if (typeof FormData !== 'undefined' && datos instanceof FormData) {
      opciones.body = datos;
    } else {
      opciones.body = JSON.stringify(datos);
    }
  }
  return peticion_ajax(endpoint, opciones);
}

/**
 * Petición genérica con soporte para opciones de lectura offline (guardable).
 * @param {string} endpoint
 * @param {object|string} opcionesOmetodo
 * @param {any} datos
 * @param {object} extra
 */
export function pedir(endpoint, opcionesOmetodo = 'GET', datos = null, extra = {}) {
  if (typeof opcionesOmetodo === 'object' && opcionesOmetodo !== null) {
    const opts = opcionesOmetodo;
    const metodo = opts.method || 'GET';
    const body = opts.body || opts.datos || null;
    return enviar(endpoint, metodo, body, opts);
  }
  return enviar(endpoint, opcionesOmetodo, datos, extra);
}

export default ajax_service;
