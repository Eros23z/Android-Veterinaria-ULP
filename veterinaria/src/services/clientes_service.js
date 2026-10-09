import { enviar } from './ajax_service';

/**
 * Convierte un objeto cliente y un archivo de foto opcional en un FormData multipart.
 * @param {Object} datos
 * @param {File|null} foto
 * @returns {FormData}
 */
function armar_cliente_form_data(datos, foto = null) {
  const formData = new FormData();
  formData.append('nombre', datos.nombre || '');
  formData.append('email', datos.email || '');
  formData.append('telefono', datos.telefono || '');
  formData.append('direccion', datos.direccion || '');

  if (datos.direccion_latitud !== undefined && datos.direccion_latitud !== null && datos.direccion_latitud !== '') {
    let lat = parseFloat(datos.direccion_latitud);
    if (!isNaN(lat)) {
      while (Math.abs(lat) > 90 && lat !== 0) lat /= 10;
      formData.append('direccion_latitud', lat.toFixed(6));
    }
  }
  if (datos.direccion_longitud !== undefined && datos.direccion_longitud !== null && datos.direccion_longitud !== '') {
    let lng = parseFloat(datos.direccion_longitud);
    if (!isNaN(lng)) {
      while (Math.abs(lng) > 180 && lng !== 0) lng /= 10;
      formData.append('direccion_longitud', lng.toFixed(6));
    }
  }

  const archivoFoto = foto || datos.foto;
  if (archivoFoto instanceof File) {
    formData.append('foto', archivoFoto);
  }

  return formData;
}

/**
 * Crea un nuevo cliente con datos y foto opcional mediante multipart/form-data.
 * @param {Object} datos - Datos del cliente (nombre, email, telefono, direccion, foto opcional).
 * @param {File|null} foto - Archivo de imagen si no está en datos.
 * @returns {Promise<Object>} Cliente creado.
 */
export async function crear_cliente(datos, foto = null) {
  const payload = armar_cliente_form_data(datos, foto);
  return await enviar('/api/clientes', 'POST', payload);
}

/**
 * Actualiza un cliente existente con datos y foto opcional mediante multipart/form-data.
 * @param {number|string} id - ID del cliente.
 * @param {Object} datos - Datos actualizados del cliente.
 * @param {File|null} foto - Archivo de imagen nueva si se capturó una.
 * @returns {Promise<Object>} Cliente actualizado.
 */
export async function actualizar_cliente(id, datos, foto = null) {
  if (!id) {
    throw new Error('El ID del cliente es obligatorio para actualizar.');
  }
  const payload = armar_cliente_form_data(datos, foto);
  return await enviar(`/api/clientes/${id}`, 'PUT', payload);
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
  armar_form_data: armar_cliente_form_data,
};

export const armar_form_data = armar_cliente_form_data;

export default clientes_service;
