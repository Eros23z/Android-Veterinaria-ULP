import { Haptics, ImpactStyle, NotificationType } from '@capacitor/haptics';
import { tema_store } from '@/stores/tema_store';

/**
 * Ejecuta una vibración háptica al tacto o acción del usuario.
 * Combina Haptics.impact (Light) con Haptics.vibrate y navigator.vibrate para máxima
 * compatibilidad y fuerza perceptible en todos los modelos de teléfonos Android.
 *
 * @param {boolean} [forzar=false] - Si es true, ejecuta la vibración sin verificar la preferencia (útil al presionar el switch).
 */
export async function vibrar_toque(forzar = false) {
  if (!forzar && !tema_store.es_vibracion_activa()) return;

  // 1. Llamar a Haptics.impact ({ style: ImpactStyle.Light }) según diseño estricto
  try {
    await Haptics.impact({ style: ImpactStyle.Light }).catch(() => {});
  } catch (_e) {
    // Silencioso
  }

  // 2. Ejecutar Haptics.vibrate (60ms) con amplitud estándar para garantizar
  // que motores de vibración ERM en Android vibren de forma claramente perceptible
  try {
    await Haptics.vibrate({ duration: 60 }).catch(() => {});
  } catch (_e) {
    // Silencioso
  }

  // 3. Fallback Web API (navigator.vibrate) para WebView y navegadores
  try {
    if (typeof navigator !== 'undefined' && typeof navigator.vibrate === 'function') {
      navigator.vibrate(60);
    }
  } catch (_e) {
    // Silencioso
  }
}

/**
 * Ejecuta una vibración háptica de error o advertencia.
 * Combina NotificationType.Error con un patrón de vibración distintivo.
 *
 * @param {boolean} [forzar=false]
 */
export async function vibrar_error(forzar = false) {
  if (!forzar && !tema_store.es_vibracion_activa()) return;

  // 1. Haptics notification error según especificación
  try {
    await Haptics.notification({ type: NotificationType.Error }).catch(() => {});
  } catch (_e) {
    // Silencioso
  }

  // 2. Vibración prolongada y perceptible en Android (250ms)
  try {
    await Haptics.vibrate({ duration: 250 }).catch(() => {});
  } catch (_e) {
    // Silencioso
  }

  // 3. Fallback Web API
  try {
    if (typeof navigator !== 'undefined' && typeof navigator.vibrate === 'function') {
      navigator.vibrate([120, 60, 120]);
    }
  } catch (_e) {
    // Silencioso
  }
}

export const vibracion_service = {
  vibrar_toque,
  vibrar_error,
};

export default vibracion_service;
