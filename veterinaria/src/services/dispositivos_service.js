import { enviar } from './ajax_service';

/**
 * Servicio para gestión y registro de dispositivos para notificaciones push en el backend.
 */
export const dispositivos_service = {
  /**
   * Registra o actualiza el token FCM del dispositivo actual en la API.
   * @param {string} token
   * @returns {Promise<{ registrado: boolean }>}
   */
  async registrar_dispositivo(token) {
    if (!token) return { registrado: false };
    return await enviar('/api/dispositivos', 'PUT', { token });
  },
};

export const registrar_dispositivo = (token) => dispositivos_service.registrar_dispositivo(token);

export default dispositivos_service;
