/**
 * Configuración de red para entorno de desarrollo y depuración.
 * Lee la variable de entorno VITE_API_URL_DEBUG o recurre a un fallback seguro.
 */

const URL_FALLBACK = 'http://localhost:5080';

export function obtener_api_url() {
  const envUrl =
    typeof import.meta !== 'undefined' && import.meta.env
      ? import.meta.env.VITE_API_URL_DEBUG
      : undefined;

  if (envUrl && typeof envUrl === 'string' && envUrl.trim() !== '') {
    return envUrl.trim().replace(/\/+$/, '');
  }
  return URL_FALLBACK;
}
