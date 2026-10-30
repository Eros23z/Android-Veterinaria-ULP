import { enviar } from './ajax_service';

/**
 * Servicio API para preparación de comandas y avance de pedidos.
 */
export const comandas_service = {
  /**
   * Obtiene la lista de pedidos en estado de preparación con sus ítems.
   * @returns {Promise<{ comandas: Array }>}
   */
  async listar() {
    return await enviar('/api/comandas', 'GET');
  },

  /**
   * Cambia el estado de un ítem individual de una comanda.
   * @param {number|string} itemId
   * @param {'pendiente'|'en_preparacion'|'listo'} nuevoEstado
   * @returns {Promise<Object>}
   */
  async mover_item(itemId, nuevoEstado) {
    if (!itemId || !nuevoEstado) {
      throw new Error('El ID de ítem y el estado son requeridos.');
    }
    return await enviar(`/api/comandas/items/${itemId}/estado`, 'PUT', { estado: nuevoEstado });
  },

  /**
   * Marca todos los ítems de un pedido como 'listo' y recalcula su estado.
   * @param {number|string} pedidoId
   * @returns {Promise<Object>}
   */
  async marcar_lista(pedidoId) {
    if (!pedidoId) {
      throw new Error('El ID del pedido es requerido.');
    }
    return await enviar(`/api/comandas/${pedidoId}/listo`, 'PUT');
  },
};

export default comandas_service;
