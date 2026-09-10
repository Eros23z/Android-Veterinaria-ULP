import { obtener_api_url } from '@/config/debug';

const TIEMPO_ESPERA_DEFECTO_MS = 8000;

/**
 * Normaliza y ejecuta peticiones HTTP usando fetch con control de timeout y captura de errores.
 *
 * @param {string} ruta - Endpoint relativo (ej: '/api/productos') o URL absoluta.
 * @param {RequestInit & { timeoutMs?: number }} opciones - Opciones de fetch y configuración adicional.
 * @returns {Promise<any>} Datos parseados de la respuesta.
 */
export async function peticion_ajax(ruta, opciones = {}) {
  const baseUrl = obtener_api_url();
  const urlCompleta = ruta.startsWith('http://') || ruta.startsWith('https://')
    ? ruta
    : `${baseUrl}${ruta.startsWith('/') ? '' : '/'}${ruta}`;

  const timeoutMs = opciones.timeoutMs || TIEMPO_ESPERA_DEFECTO_MS;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  const cabeceras = {
    'Accept': 'application/json',
    ...(opciones.body ? { 'Content-Type': 'application/json' } : {}),
    ...opciones.headers,
  };

  try {
    const respuesta = await fetch(urlCompleta, {
      ...opciones,
      headers: cabeceras,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    if (!respuesta.ok) {
      let detalleError = `Error HTTP ${respuesta.status}: ${respuesta.statusText}`;
      try {
        const errorJson = await respuesta.json();
        if (errorJson?.mensaje) {
          detalleError = errorJson.mensaje;
        } else if (errorJson?.title) {
          detalleError = errorJson.title;
        }
      } catch {
        // En caso de que el cuerpo del error no sea JSON
      }
      throw new Error(detalleError);
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

export default ajax_service;
