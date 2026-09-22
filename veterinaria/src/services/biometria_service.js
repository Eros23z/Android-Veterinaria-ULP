import { BiometricAuth } from '@aparajita/capacitor-biometric-auth';

/**
 * Comprueba si el dispositivo soporta biometría y si el usuario tiene datos biométricos configurados.
 * @returns {Promise<{ disponible: boolean, tipo: number, mensaje: string }>}
 */
export async function verificar_biometria() {
  try {
    const info = await BiometricAuth.checkBiometry();
    return {
      disponible: !!info.isAvailable,
      tipo: info.biometryType,
      mensaje: info.reason || '',
    };
  } catch (error) {
    console.warn('Error al verificar biometría:', error);
    return {
      disponible: false,
      tipo: 0,
      mensaje: error?.message || 'Biometría no disponible',
    };
  }
}

/**
 * Solicita autenticación biométrica al usuario con el cuadro de diálogo del sistema.
 * @param {string} razon - Motivo de autenticación a mostrar en el modal del sistema.
 * @returns {Promise<boolean>}
 */
export async function autenticar_biometria(razon = 'Confirme su identidad para acceder a la aplicación') {
  try {
    await BiometricAuth.authenticate({
      reason: razon,
      cancelTitle: 'Cancelar',
      allowDeviceCredential: true,
    });
    return true;
  } catch (error) {
    console.warn('Fallo o cancelación en autenticación biométrica:', error);
    return false;
  }
}

/**
 * Comprueba si el dispositivo soporta biometría y si el usuario tiene datos biométricos configurados.
 * @returns {Promise<boolean>}
 */
export async function verificar_disponibilidad() {
  const info = await verificar_biometria();
  return info.disponible;
}

/**
 * Solicita autenticación biométrica al usuario con el mensaje especificado.
 * @param {string} razon
 * @returns {Promise<boolean>}
 */
export async function autenticar(razon = 'Confirma tu identidad para entrar') {
  return await autenticar_biometria(razon);
}

export const biometria_service = {
  verificar_biometria,
  verificar_disponibilidad,
  autenticar_biometria,
  autenticar,
};

export default biometria_service;

