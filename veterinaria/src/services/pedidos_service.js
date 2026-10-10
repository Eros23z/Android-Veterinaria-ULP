import { pedir, enviar } from './ajax_service';
import { armarConsulta } from '../utils/consulta';

/**
 * Servicio de API para Pedidos con soporte para copias locales offline.
 */
export const pedidos_service = {
  /**
   * Obtiene el listado de pedidos paginado con filtros.
   * Permite guardar copia local para uso offline.
   * @param {object} params
   */
  async listar_pedidos(params = {}) {
    const query = armarConsulta(params);
    return await pedir(`/api/pedidos${query}`, {
      method: 'GET',
      guardable: true,
    });
  },

  /**
   * Obtiene el detalle de un pedido específico por ID.
   * Permite guardar copia local para uso offline.
   * @param {number|string} id
   */
  async obtener_pedido(id) {
    return await pedir(`/api/pedidos/${id}`, {
      method: 'GET',
      guardable: true,
    });
  },

  /**
   * Cambia el estado de un pedido según la máquina de estados.
   * @param {number|string} id
   * @param {string} nuevo_estado
   */
  async cambiar_estado(id, nuevo_estado) {
    return await enviar(`/api/pedidos/${id}/estado`, 'PUT', { estado: nuevo_estado });
  },

  /**
   * Crea un nuevo pedido con items y entrega opcional.
   * @param {object} datos
   */
  async crear_pedido(datos) {
    return await enviar('/api/pedidos', 'POST', datos);
  },
};

export default pedidos_service;
