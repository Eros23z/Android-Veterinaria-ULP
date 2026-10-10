import { ajax_service, pedir } from './ajax_service';
import { armarConsulta } from '../utils/consulta';

/**
 * Servicio para consulta y gestión de productos del catálogo.
 */
export const productos_service = {
  /**
   * Lista los productos con paginación y filtros. Soporta copia offline local.
   * @param {object} params
   */
  async listar_productos(params = {}) {
    const query = armarConsulta(params);
    return await pedir(`/api/productos${query}`, {
      method: 'GET',
      guardable: true,
    });
  },

  /**
   * Obtiene el resumen de productos, categorías y métricas de disponibilidad.
   */
  async obtener_resumen() {
    return await pedir('/api/productos/resumen', {
      method: 'GET',
      guardable: true,
    });
  },

  /**
   * Obtiene un producto específico por su ID.
   * @param {number|string} id
   */
  async obtener_por_id(id) {
    return await pedir(`/api/productos/${id}`, {
      method: 'GET',
      guardable: true,
    });
  },

  /**
   * Verifica la conectividad y disponibilidad del backend.
   */
  async verificar_salud() {
    return await ajax_service.get('/health');
  }
};

export const listar_productos = (p) => productos_service.listar_productos(p);
export const obtener_resumen = () => productos_service.obtener_resumen();
export const obtener_por_id = (id) => productos_service.obtener_por_id(id);

export default productos_service;
