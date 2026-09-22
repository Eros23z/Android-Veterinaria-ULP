import { enviar } from './ajax_service';
import { armarConsulta } from '../utils/consulta';

export async function listar_usuarios(parametros = {}) {
  const query = armarConsulta(parametros);
  return await enviar(`/api/usuarios${query}`, 'GET');
}

export async function listar_roles() {
  return await enviar('/api/roles', 'GET');
}

export async function crear_usuario(datos) {
  return await enviar('/api/usuarios', 'POST', datos);
}

export async function actualizar_rol(id, rolId) {
  if (!id) throw new Error('El ID de usuario es obligatorio.');
  return await enviar(`/api/usuarios/${id}/rol`, 'PUT', { rol_id: rolId });
}

export async function eliminar_usuario(id) {
  if (!id) throw new Error('El ID de usuario es obligatorio.');
  return await enviar(`/api/usuarios/${id}`, 'DELETE');
}

export const usuarios_service = {
  listar_usuarios,
  listar_roles,
  crear_usuario,
  actualizar_rol,
  eliminar_usuario,
};

export default usuarios_service;
