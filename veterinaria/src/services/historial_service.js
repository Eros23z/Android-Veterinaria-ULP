import { enviar } from './ajax_service';
import { armarConsulta } from '../utils/consulta';

/**
 * Servicio API para consulta de trazabilidad y auditoría de eventos.
 */
export const historial_service = {
  /**
   * Obtiene eventos de auditoría con filtros, resumen y paginación.
   * @param {{ tipo?: string, busqueda?: string, solo_mios?: boolean, pagina?: number, tamano?: number }} parametros
   * @returns {Promise<{ historial: Array, resumen: Object, paginacion: Object }>}
   */
  async obtener_historial(parametros = {}) {
    const params = { ...parametros };
    if (params.solo_mios === false) {
      delete params.solo_mios;
    }
    const query = armarConsulta(params);
    return await enviar(`/api/historial${query}`, 'GET');
  },
};

export default historial_service;
