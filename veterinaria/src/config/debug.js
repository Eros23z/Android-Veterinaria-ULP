const api_url_produccion = 'https://veterinaria-ulp.runasp.net';

export const debug_config = {
  debug_activado: import.meta.env.VITE_DEBUG_ACTIVADO != 'false',
  api_url_debug: (import.meta.env.VITE_API_URL_DEBUG || 'http://localhost:5080').replace(/\/+$/, '')
};

export function obtener_api_url() {
  const api_url_entorno = (import.meta.env.VITE_API_URL || '').replace(/\/+$/, '');
  if (debug_config.debug_activado) return debug_config.api_url_debug;
  return api_url_entorno || api_url_produccion;
}
