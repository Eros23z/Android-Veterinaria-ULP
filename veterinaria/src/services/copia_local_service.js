const PREFIJO = 'veterinaria_copia:';

/**
 * Guarda una copia en localStorage de la respuesta de un endpoint para uso offline.
 * @param {string} endpoint
 * @param {any} respuesta
 */
export function guardar_copia(endpoint, respuesta) {
  if (!endpoint) return;
  try {
    const clave = `${PREFIJO}${endpoint}`;
    localStorage.setItem(clave, JSON.stringify(respuesta));
  } catch (error) {
    console.warn(`Error al guardar copia local para ${endpoint}:`, error);
  }
}

/**
 * Obtiene la copia guardada de un endpoint desde localStorage.
 * @param {string} endpoint
 * @returns {any|null}
 */
export function obtener_copia(endpoint) {
  if (!endpoint) return null;
  try {
    const clave = `${PREFIJO}${endpoint}`;
    const datos = localStorage.getItem(clave);
    if (!datos) return null;
    return JSON.parse(datos);
  } catch (error) {
    console.warn(`Error al leer copia local de ${endpoint}:`, error);
    return null;
  }
}

/**
 * Elimina todas las copias locales que coincidan con el prefijo (al cerrar sesión).
 */
export function borrar_copias() {
  try {
    const clavesParaEliminar = [];
    for (let i = 0; i < localStorage.length; i++) {
      const clave = localStorage.key(i);
      if (clave && clave.startsWith(PREFIJO)) {
        clavesParaEliminar.push(clave);
      }
    }
    for (const clave of clavesParaEliminar) {
      localStorage.removeItem(clave);
    }
  } catch (error) {
    console.warn('Error al borrar copias locales:', error);
  }
}

export const copia_local_service = {
  guardar_copia,
  obtener_copia,
  borrar_copias,
};

export default copia_local_service;
