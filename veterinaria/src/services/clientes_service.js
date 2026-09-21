import { enviar } from './ajax_service';

/**
 * Crea un nuevo cliente.
 * @param {Object} datos - Datos del cliente (nombre, email, telefono, direccion).
 * @returns {Promise<Object>} Cliente creado.
 */
export async function crear_cliente(datos) {
  return await enviar('/api/clientes', 'POST', datos);
}

/**
 * Actualiza un cliente existente.
 * @param {number|string} id - ID del cliente.
 * @param {Object} datos - Datos actualizados del cliente.
 * @returns {Promise<Object>} Cliente actualizado.
 */
export async function actualizar_cliente(id, datos) {
  if (!id) {
    throw new Error('El ID del cliente es obligatorio para actualizar.');
  }
  return await enviar(`/api/clientes/${id}`, 'PUT', datos);
}

/**
 * Realiza la baja lógica de un cliente.
 * @param {number|string} id - ID del cliente.
 * @returns {Promise<any>}
 */
export async function eliminar_cliente(id) {
  if (!id) {
    throw new Error('El ID del cliente es obligatorio para eliminar.');
  }
  return await enviar(`/api/clientes/${id}`, 'DELETE');
}

export const clientes_service = {
  crear_cliente,
  actualizar_cliente,
  eliminar_cliente,
};

export default clientes_service;
