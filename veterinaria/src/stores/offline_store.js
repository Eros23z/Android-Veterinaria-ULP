import { reactive } from 'vue';
import { Network } from '@capacitor/network';
import { toastController } from '@ionic/vue';
import { obtener_token } from '../services/token_service';
import { pedidos_service } from '../services/pedidos_service';
import { vibrar_error } from '../services/vibracion_service';

const CLAVE_COLA = 'veterinaria_cola_offline';

function cargar_cola_inicial() {
  try {
    const guardado = localStorage.getItem(CLAVE_COLA);
    return guardado ? JSON.parse(guardado) : [];
  } catch (error) {
    console.error('Error al leer cola offline de localStorage:', error);
    return [];
  }
}

function persistir_cola(cola) {
  try {
    localStorage.setItem(CLAVE_COLA, JSON.stringify(cola));
  } catch (error) {
    console.error('Error al persistir cola offline en localStorage:', error);
  }
}

export const offline_store = reactive({
  en_linea: typeof navigator !== 'undefined' ? navigator.onLine !== false : true,
  get sin_conexion() {
    return !this.en_linea;
  },
  sincronizando: false,
  cola: cargar_cola_inicial(),

  /**
   * Encola una acción de cambio de estado para sincronizar posteriormente.
   * @param {number|string} pedido_id
   * @param {string} nuevo_estado
   */
  encolar(pedido_id, nuevo_estado) {
    const item = {
      id: Date.now(),
      pedido_id,
      nuevo_estado,
    };
    this.cola.push(item);
    persistir_cola(this.cola);
    return item;
  },

  /**
   * Limpia toda la cola de salida guardada en memoria y localStorage.
   */
  limpiar_cola() {
    this.cola = [];
    try {
      localStorage.removeItem(CLAVE_COLA);
    } catch (_e) {
      // Ignorar errores en limpieza
    }
  },

  /**
   * Recorre la cola y envía los cambios pendientes si hay conectividad.
   */
  async sincronizar() {
    if (this.sincronizando || this.cola.length === 0 || !obtener_token()) {
      return;
    }

    this.sincronizando = true;
    let cambios_enviados = 0;

    try {
      const items = [...this.cola];
      for (const item of items) {
        if (!this.en_linea) {
          break;
        }

        try {
          await pedidos_service.cambiar_estado(item.pedido_id, item.nuevo_estado);
          this.cola = this.cola.filter((c) => c.id !== item.id);
          persistir_cola(this.cola);
          cambios_enviados++;
        } catch (error) {
          const status = error?.status;

          // Si falla por falta de red (status 0), 401 o >= 500: conserva el item en la cola
          if (!status || status === 0 || status === 401 || status >= 500) {
            if (!status || status === 0) {
              this.en_linea = false;
              break;
            }
            continue;
          }

          // Si la API rechaza con 400 o 409: lo quita de la cola y notifica error
          if (status === 400 || status === 409) {
            this.cola = this.cola.filter((c) => c.id !== item.id);
            persistir_cola(this.cola);
            vibrar_error();
            try {
              const toast = await toastController.create({
                message: `No se pudo cambiar el estado del pedido #${item.pedido_id}: ${error.message || 'Datos incompatibles'}`,
                duration: 4000,
                color: 'warning',
                position: 'bottom',
              });
              await toast.present();
            } catch (_t) {
              // Ignorar fallo en toast
            }
          } else {
            // Otros errores no recuperables del cliente: descartar
            this.cola = this.cola.filter((c) => c.id !== item.id);
            persistir_cola(this.cola);
          }
        }
      }

      // Si algo se envió, recarga la lista de pedidos en pedidos_store
      if (cambios_enviados > 0) {
        try {
          const { pedidos_store } = await import('./pedidos_store');
          if (pedidos_store && typeof pedidos_store.cargar_pedidos === 'function') {
            await pedidos_store.cargar_pedidos();
          }
        } catch (errRecarga) {
          console.warn('Error al recargar pedidos tras sincronización:', errRecarga);
        }
      }
    } finally {
      this.sincronizando = false;
    }
  },
});

/**
 * Inicializa la escucha de cambios de red vía Capacitor Network y sincroniza si vuelve la conexión.
 */
export async function inicializar_red() {
  try {
    const estado = await Network.getStatus();
    offline_store.en_linea = estado.connected;
    if (offline_store.en_linea) {
      offline_store.sincronizar();
    }
  } catch (error) {
    console.warn('No se pudo obtener estado inicial de Network, usando fallback navigator.onLine:', error);
    offline_store.en_linea = navigator.onLine !== false;
  }

  Network.addListener('networkStatusChange', (estado) => {
    offline_store.en_linea = estado.connected;
    if (offline_store.en_linea) {
      offline_store.sincronizar();
    }
  });
}

export default offline_store;
