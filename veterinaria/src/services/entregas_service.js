import { enviar } from './ajax_service';
import { armarConsulta } from '../utils/consulta';

/**
 * Servicio API para entregas y logística de reparto a domicilio.
 */
export const entregas_service = {
  /**
   * Obtiene la lista de entregas con filtros, resumen de contadores y paginación.
   * @param {{ estados?: string, solo_mias?: boolean, pagina?: number, tamano?: number }} parametros
   * @returns {Promise<{ entregas: Array, resumen: Object, pagina: Object }>}
   */
  async obtener_entregas(parametros = {}) {
    const params = { ...parametros };
    if (params.solo_mias === false) {
      delete params.solo_mias;
    }
    const query = armarConsulta(params);
    return await enviar(`/api/entregas${query}`, 'GET');
  },

  /**
   * Asigna una entrega al usuario autenticado (si repartidor_id es null) o a un tercero.
   * @param {number|string} id - ID de la entrega.
   * @param {number|string|null} repartidor_id - ID del repartidor opcional.
   * @returns {Promise<Object>}
   */
  async asignar(id, repartidor_id = null) {
    if (!id) throw new Error('El ID de la entrega es obligatorio.');
    const payload = repartidor_id ? { repartidor_id } : null;
    return await enviar(`/api/entregas/${id}/asignar`, 'PUT', payload);
  },

  /**
   * Cambia el estado de una entrega aplicando la máquina de estados.
   * @param {number|string} id - ID de la entrega.
   * @param {string} nuevo_estado - Nuevo estado (ej: 'en_camino', 'entregada', 'cancelada').
   * @returns {Promise<Object>}
   */
  async cambiar_estado(id, nuevo_estado) {
    if (!id || !nuevo_estado) {
      throw new Error('El ID y el nuevo estado son obligatorios.');
    }
    return await enviar(`/api/entregas/${id}/estado`, 'PUT', { estado: nuevo_estado });
  },
};

export default entregas_service;
