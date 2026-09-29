import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';
import { Capacitor } from '@capacitor/core';

/**
 * Indica si el dispositivo cuenta con capacidades nativas de cámara.
 * @returns {boolean}
 */
export function camara_disponible() {
  return Capacitor.isNativePlatform();
}

/**
 * Consulta y solicita permisos de cámara o galería en tiempo de ejecución.
 * @param {'camara' | 'galeria'} origen
 * @returns {Promise<{ concedido: boolean, mensaje: string | null }>}
 */
export async function asegurar_permiso(origen = 'camara') {
  try {
    const permisos = await Camera.checkPermissions();
    const estado = origen === 'camara' ? permisos.camera : permisos.photos;

    if (estado === 'granted') {
      return { concedido: true, mensaje: null };
    }

    const solicitado = await Camera.requestPermissions({
      permissions: origen === 'camara' ? ['camera'] : ['photos'],
    });

    const nuevoEstado = origen === 'camara' ? solicitado.camera : solicitado.photos;
    if (nuevoEstado === 'granted') {
      return { concedido: true, mensaje: null };
    }

    return {
      concedido: false,
      mensaje: 'Permiso denegado. Para utilizar la cámara o galería, habilite el acceso en los Ajustes del dispositivo.',
    };
  } catch (error) {
    console.warn('Error al verificar/solicitar permisos de cámara:', error);
    return {
      concedido: false,
      mensaje: error.message || 'No se pudieron verificar los permisos de la cámara.',
    };
  }
}

/**
 * Captura o selecciona una foto desde la cámara o galería del dispositivo.
 * Convierte el resultado en un objeto File estándar listo para envío multipart.
 *
 * @param {'camara' | 'galeria'} origen
 * @returns {Promise<{ ok: boolean, archivo: File | null, url_preview: string | null, mensaje: string | null }>}
 */
export async function tomar_foto(origen = 'camara') {
  try {
    const fuente = origen === 'galeria' ? CameraSource.Photos : CameraSource.Camera;

    if (Capacitor.isNativePlatform()) {
      const permiso = await asegurar_permiso(origen);
      if (!permiso.concedido) {
        return {
          ok: false,
          archivo: null,
          url_preview: null,
          mensaje: permiso.mensaje,
        };
      }
    }

    const foto = await Camera.getPhoto({
      resultType: CameraResultType.Uri,
      source: fuente,
      quality: 90,
      width: 1600,
      allowEditing: false,
    });

    if (!foto || !foto.webPath) {
      return {
        ok: false,
        archivo: null,
        url_preview: null,
        mensaje: 'No se obtuvo la imagen seleccionada.',
      };
    }

    // Convertir webPath a File estándar mediante fetch
    const respuesta = await fetch(foto.webPath);
    const blob = await respuesta.blob();
    const formato = foto.format || 'jpg';
    const nombreArchivo = `foto_cliente_${Date.now()}.${formato}`;
    const archivo = new File([blob], nombreArchivo, {
      type: blob.type || `image/${formato}`,
    });

    return {
      ok: true,
      archivo,
      url_preview: foto.webPath,
      mensaje: null,
    };
  } catch (error) {
    const mensajeTexto = error?.message || '';
    const cancelado = mensajeTexto.includes('User cancelled') || mensajeTexto.includes('cancelled');

    return {
      ok: false,
      archivo: null,
      url_preview: null,
      mensaje: cancelado ? null : (mensajeTexto || 'Error al capturar la imagen.'),
    };
  }
}

export const camara_service = {
  camara_disponible,
  asegurar_permiso,
  tomar_foto,
};

export default camara_service;
