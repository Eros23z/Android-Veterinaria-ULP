import { LocalNotifications } from '@capacitor/local-notifications';
import { PushNotifications } from '@capacitor/push-notifications';
import { Capacitor } from '@capacitor/core';

/**
 * Servicio para notificaciones locales inmediatas y push notifications nativas.
 */
export const notificaciones_nativas_service = {
  /**
   * Programa o dispara una notificación local inmediata en el dispositivo.
   * @param {string} titulo - Título de la notificación.
   * @param {string} mensaje - Cuerpo del mensaje.
   * @returns {Promise<boolean>} Retorna true si se programó exitosamente.
   */
  async notificar_evento_local(titulo, mensaje) {
    try {
      if (!Capacitor.isPluginAvailable('LocalNotifications')) {
        console.warn('LocalNotifications plugin no disponible en esta plataforma.');
        return false;
      }

      // Comprobar / solicitar permisos de notificaciones locales
      const permisos = await LocalNotifications.checkPermissions();
      if (permisos.display !== 'granted') {
        const solicitud = await LocalNotifications.requestPermissions();
        if (solicitud.display !== 'granted') {
          console.warn('Permiso de notificaciones locales no concedido.');
          return false;
        }
      }

      // ID entero de 32 bits seguro para Android
      const idNotificacion = Math.floor(Date.now() % 2147483647);

      await LocalNotifications.schedule({
        notifications: [
          {
            id: idNotificacion,
            title: titulo,
            body: mensaje,
            schedule: { at: new Date(Date.now() + 100) },
            sound: undefined,
            attachments: undefined,
            actionTypeId: '',
            extra: null,
          },
        ],
      });

      return true;
    } catch (error) {
      console.warn('Error al disparar notificación local:', error);
      return false;
    }
  },

  /**
   * Inicializa y registra la app para Push Notifications con FCM.
   * Diseñado con manejo resiliente: si falta google-services.json o falla en web/emulador,
   * captura el error limpiamente sin romper la aplicación.
   * @returns {Promise<string|null>} Retorna el token de registro si fue exitoso.
   */
  async preparar_push() {
    try {
      if (!Capacitor.isNativePlatform() || !Capacitor.isPluginAvailable('PushNotifications')) {
        console.info('PushNotifications omitido: plataforma no nativa o plugin ausente.');
        return null;
      }

      const permisos = await PushNotifications.checkPermissions();
      let estado = permisos.receive;

      if (estado !== 'granted') {
        const solicitud = await PushNotifications.requestPermissions();
        estado = solicitud.receive;
      }

      if (estado !== 'granted') {
        console.warn('Permisos de Push Notifications no concedidos.');
        return null;
      }

      // Registrar listeners para captura de token y mensajes entrantes
      await PushNotifications.addListener('registration', (token) => {
        console.info('Token de Push FCM registrado:', token.value);
      });

      await PushNotifications.addListener('registrationError', (error) => {
        console.warn('Error en registro de Push Notifications (ej: falta google-services.json):', error);
      });

      await PushNotifications.addListener('pushNotificationReceived', async (notif) => {
        console.info('Notificación Push recibida en primer plano:', notif);
        // Si la app está abierta en primer plano, mostrar aviso local para visibilidad
        if (notif.title || notif.body) {
          await notificaciones_nativas_service.notificar_evento_local(
            notif.title || 'Veterinaria San Roque',
            notif.body || ''
          );
        }
      });

      await PushNotifications.register();
      return true;
    } catch (error) {
      // Captura segura: nunca rompe el ciclo de vida del frontend
      console.warn('Aviso: Push Notifications no inicializado (normal si no está configurado FCM):', error?.message || error);
      return null;
    }
  },
};

export default notificaciones_nativas_service;
