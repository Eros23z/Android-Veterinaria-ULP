import { enviar } from './ajax_service';
import { armarConsulta } from '../utils/consulta';

/**
 * Servicio API para notificaciones del usuario autenticado.
 */
export const notificaciones_service = {
  /**
   * Obtiene la lista paginada de notificaciones y resumen de no leídas.
   * @param {{ pagina?: number, tamano?: number }} parametros
   * @returns {Promise<{ notificaciones: Array, resumen: { total: number, no_leidas: number }, pagina: Object }>}
   */
  async obtener_notificaciones(parametros = {}) {
    const query = armarConsulta(parametros);
    return await enviar(`/api/notificaciones${query}`, 'GET');
  },

  /**
   * Marca una notificación específica como leída.
   * @param {number|string} id
   * @returns {Promise<Object>}
   */
  async marcar_leida(id) {
    if (!id) throw new Error('El ID de notificación es obligatorio.');
    return await enviar(`/api/notificaciones/${id}/leer`, 'PUT');
  },

  /**
   * Marca todas las notificaciones pendientes del usuario como leídas.
   * @returns {Promise<Object>}
   */
  async marcar_todas_leidas() {
    return await enviar('/api/notificaciones/marcar-todas', 'PUT');
  },
};

export default notificaciones_service;
