import { obtener_api_url } from '@/config/debug';
import { token_service } from './token_service';

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
 * Normaliza y ejecuta peticiones HTTP usando fetch con control de timeout, inyección de token y renovación automática ante 401.
 *
 * @param {string} ruta - Endpoint relativo (ej: '/api/productos') o URL absoluta.
 * @param {RequestInit & { timeoutMs?: number }} opciones - Opciones de fetch y configuración adicional.
 * @param {boolean} esReintento - Bandera interna para evitar bucles de renovación.
 * @returns {Promise<any>} Datos parseados de la respuesta.
 */
export async function peticion_ajax(ruta, opciones = {}, esReintento = false) {
  const baseUrl = obtener_api_url();
  const urlCompleta = ruta.startsWith('http://') || ruta.startsWith('https://')
    ? ruta
    : `${baseUrl}${ruta.startsWith('/') ? '' : '/'}${ruta}`;

  const timeoutMs = opciones.timeoutMs || TIEMPO_ESPERA_DEFECTO_MS;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  const tokenActual = token_service.obtener_token();

  const cabeceras = {
    'Accept': 'application/json',
    ...(opciones.body ? { 'Content-Type': 'application/json' } : {}),
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
          throw errorRenovacion;
        } finally {
          promesaRenovacion = null;
        }
      }
    }

    if (!respuesta.ok) {
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
    if (contentType && contentType.includes('application/json')) {
      return await respuesta.json();
    }
    return await respuesta.text();
  } catch (error) {
    clearTimeout(timeoutId);

    if (error.name === 'AbortError') {
      throw new Error(`Tiempo de espera agotado (${timeoutMs / 1000}s) al contactar al servidor.`);
    }

    if (error.message?.includes('Failed to fetch') || error.name === 'TypeError') {
      throw new Error(`No se pudo establecer conexión con el servidor (${baseUrl}). Verifique la red y que la API esté activa.`);
    }

    throw error;
  }
}

export const ajax_service = {
  get: (ruta, opciones = {}) => peticion_ajax(ruta, { ...opciones, method: 'GET' }),
  post: (ruta, datos, opciones = {}) =>
    peticion_ajax(ruta, {
      ...opciones,
      method: 'POST',
      body: JSON.stringify(datos),
    }),
};

export async function enviar(endpoint, metodo = 'GET', datos = null) {
  const opciones = { method: metodo };
  if (datos && (metodo === 'POST' || metodo === 'PUT' || metodo === 'PATCH')) {
    opciones.body = JSON.stringify(datos);
  }
  return peticion_ajax(endpoint, opciones);
}

export default ajax_service;
