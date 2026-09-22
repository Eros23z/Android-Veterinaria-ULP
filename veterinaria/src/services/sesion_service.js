import { enviar } from './ajax_service';

export async function login(email, password) {
  return await enviar('/api/sesion/login', 'POST', { email, password });
}

export async function refresh(refreshToken) {
  return await enviar('/api/sesion/refresh', 'POST', { refresh_token: refreshToken });
}

export async function logout(refreshToken) {
  return await enviar('/api/sesion/logout', 'POST', { refresh_token: refreshToken });
}

export async function obtener_perfil() {
  return await enviar('/api/sesion/yo', 'GET');
}

export const sesion_service = {
  login,
  refresh,
  logout,
  obtener_perfil,
};

export default sesion_service;
