import { enviar, pedir } from './ajax_service';

export async function login(email, password) {
  return await enviar('/api/sesion/login', 'POST', { email, password });
}

export async function refresh(refreshToken) {
  return await enviar('/api/sesion/refresh', 'POST', { refresh_token: refreshToken });
}

export async function logout(refreshToken) {
  return await enviar('/api/sesion/logout', 'POST', { refresh_token: refreshToken });
}

/**
 * Obtiene el usuario autenticado actual con soporte para copia local offline.
 */
export async function obtener_mi_usuario() {
  return await pedir('/api/sesion/yo', {
    method: 'GET',
    guardable: true,
  });
}

export const obtener_perfil = obtener_mi_usuario;

export const sesion_service = {
  login,
  refresh,
  logout,
  obtener_mi_usuario,
  obtener_perfil,
};

export default sesion_service;
