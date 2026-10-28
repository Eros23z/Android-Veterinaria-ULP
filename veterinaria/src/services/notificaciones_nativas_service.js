import { LocalNotifications } from '@capacitor/local-notifications';
import { PushNotifications } from '@capacitor/push-notifications';
import { Capacitor } from '@capacitor/core';

let _tokenPushActual = null;
let _inicializado = false;
let _acciones = {};

function navegarAlPedido(codigo) {
  if (!codigo) return;
  import('@/router')
    .then(({ default: router }) => {
      router.push(`/app/pedidos?buscar=${encodeURIComponent(codigo)}`);
    })
    .catch((err) => console.warn('Error navegando a pedido desde notificación:', err));
}

/**
 * Vincula el token de FCM del dispositivo con la cuenta del usuario autenticado en la API.
 * @param {string|null} token
 */
export async function vincular_dispositivo(token = null) {
  const t = token || _tokenPushActual;
  if (!t) return false;

  try {
    const { sesion_store } = await import('@/stores/sesion_store');
    if (sesion_store.autenticado) {
      const { registrar_dispositivo } = await import('./dispositivos_service');
      await registrar_dispositivo(t);
      return true;
    }
  } catch (error) {
    console.warn('No se pudo vincular el token del dispositivo con la API:', error);
  }

  return false;
}

/**
 * Obtiene el token de FCM actualmente registrado en memoria o localStorage.
 * @returns {string|null}
 */
export function obtener_token_push() {
  if (_tokenPushActual) return _tokenPushActual;
  try {
    return localStorage.getItem('vet_push_token') || null;
  } catch {
    return null;
  }
}

/**
 * Dispara una notificación local inmediata en el dispositivo.
 */
export async function notificar_evento_local(titulo, mensaje, data = null) {
  return await notificaciones_nativas_service.notificar_evento_local(titulo, mensaje, data);
}

/**
 * Inicializa canales, listeners y registra la app para Push Notifications con FCM.
 * @param {object} acciones - Callbacks: { al_registrar, al_recibir, al_tocar }
 */
export async function inicializar_notificaciones_nativas(acciones = {}) {
  return await notificaciones_nativas_service.inicializar_notificaciones_nativas(acciones);
}

/**
 * Servicio para notificaciones locales inmediatas y push notifications nativas (Unidad 9).
 */
export const notificaciones_nativas_service = {
  vincular_dispositivo,
  obtener_token_push,
  notificar_evento_local,
  inicializar_notificaciones_nativas,

  /**
   * Programa o dispara una notificación local inmediata en el dispositivo.
   * @param {string} titulo - Título de la notificación.
   * @param {string} mensaje - Cuerpo del mensaje.
   * @param {object|null} data - Datos adicionales (ej: { pedido_codigo: 'PED-0001' }).
   * @returns {Promise<boolean>} Retorna true si se programó exitosamente.
   */
  async notificar_evento_local(titulo, mensaje, data = null) {
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

      // Asegurar canal de avisos en Android
      if (Capacitor.getPlatform() === 'android') {
        try {
          await LocalNotifications.createChannel({
            id: 'avisos',
            name: 'Avisos de pedidos',
            importance: 5,
            visibility: 1,
            vibration: true,
          });
        } catch (_e) {
          // Ignorar si el canal ya existe
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
            channelId: 'avisos',
            schedule: { at: new Date(Date.now() + 100) },
            extra: data || null,
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
   * Inicializa canales, listeners y registra la app para Push Notifications con FCM.
   * Resiliente ante entornos sin FCM, web o sin Google Services.
   * @param {object} acciones - Callbacks: { al_registrar, al_recibir, al_tocar }
   * @returns {Promise<boolean|null>} Retorna true si el registro fue exitoso.
   */
  async inicializar_notificaciones_nativas(acciones = {}) {
    if (acciones && typeof acciones === 'object') {
      _acciones = { ..._acciones, ...acciones };
    }

    try {
      if (!Capacitor.isNativePlatform() || !Capacitor.isPluginAvailable('PushNotifications')) {
        console.info('PushNotifications omitido: plataforma no nativa o plugin ausente.');
        return null;
      }

      // Crear canal de alta importancia en Android
      if (Capacitor.getPlatform() === 'android' && Capacitor.isPluginAvailable('LocalNotifications')) {
        try {
          await LocalNotifications.createChannel({
            id: 'avisos',
            name: 'Avisos de pedidos',
            importance: 5,
            visibility: 1,
            vibration: true,
          });
        } catch (e) {
          console.debug('Aviso al configurar canal:', e);
        }
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

      if (!_inicializado) {
        _inicializado = true;

        // Listener: Captura y guardado de token de registro FCM
        await PushNotifications.addListener('registration', async (token) => {
          _tokenPushActual = token.value;
          try {
            localStorage.setItem('vet_push_token', token.value);
          } catch {}

          if (_acciones && typeof _acciones.al_registrar === 'function') {
            try {
              await _acciones.al_registrar(token.value);
            } catch (errReg) {
              console.warn('Error en callback al_registrar:', errReg);
            }
          } else {
            await vincular_dispositivo(token.value);
          }
        });

        await PushNotifications.addListener('registrationError', (error) => {
          console.warn('Error en registro de Push Notifications (ej: falta google-services.json):', error);
        });

        // Listener: Push entrante con app en primer plano -> mostrar notificación local y ejecutar al_recibir
        await PushNotifications.addListener('pushNotificationReceived', async (push) => {
          await notificar_evento_local(
            push.title || 'Veterinaria San Roque',
            push.body || '',
            push.data || {}
          );
          if (_acciones && typeof _acciones.al_recibir === 'function') {
            try {
              _acciones.al_recibir();
            } catch (errRecibir) {
              console.warn('Error al ejecutar acciones.al_recibir():', errRecibir);
            }
          }
        });

        // Listener: Clic en notificación push de segundo plano / cerrada
        await PushNotifications.addListener('pushNotificationActionPerformed', (action) => {
          const data = action.notification?.data || {};
          if (_acciones && typeof _acciones.al_tocar === 'function') {
            try {
              _acciones.al_tocar(data);
            } catch (errTocar) {
              console.warn('Error en callback al_tocar:', errTocar);
            }
          }
          const codigo = data?.pedido_codigo;
          if (codigo) {
            navegarAlPedido(codigo);
          }
        });

        // Listener: Clic en notificación local en primer plano
        if (Capacitor.isPluginAvailable('LocalNotifications')) {
          await LocalNotifications.addListener('localNotificationActionPerformed', (action) => {
            const extra = action.notification?.extra || {};
            if (_acciones && typeof _acciones.al_tocar === 'function') {
              try {
                _acciones.al_tocar(extra);
              } catch (errTocar) {
                console.warn('Error en callback al_tocar:', errTocar);
              }
            }
            const codigo = extra?.pedido_codigo;
            if (codigo) {
              navegarAlPedido(codigo);
            }
          });
        }
      }

      await PushNotifications.register();
      return true;
    } catch (error) {
      console.warn('Aviso: Push Notifications no inicializado (normal si no está configurado FCM):', error?.message || error);
      return null;
    }
  },

  preparar_push(acciones = {}) {
    return this.inicializar_notificaciones_nativas(acciones);
  },
};

export default notificaciones_nativas_service;
