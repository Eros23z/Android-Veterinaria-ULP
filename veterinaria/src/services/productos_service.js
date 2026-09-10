import { ajax_service } from './ajax_service';

export const productos_service = {
  /**
   * Obtiene el resumen de productos, categorías y métricas de disponibilidad.
   */
  async obtener_resumen() {
    return await ajax_service.get('/api/productos/resumen');
  },

  /**
   * Obtiene un producto específico por su ID.
   * @param {number|string} id
   */
  async obtener_por_id(id) {
    return await ajax_service.get(`/api/productos/${id}`);
  },

  /**
   * Verifica la conectividad y disponibilidad del backend.
   */
  async verificar_salud() {
    return await ajax_service.get('/health');
  }
};

export default productos_service;
