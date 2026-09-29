import { Filesystem, Directory } from '@capacitor/filesystem';
import { Share } from '@capacitor/share';
import { Capacitor } from '@capacitor/core';

/**
 * Convierte un objeto Blob en una cadena codificada en Base64.
 * @param {Blob} blob
 * @returns {Promise<string>}
 */
function blob_a_base64(blob) {
  return new Promise((resolve, reject) => {
    const lector = new FileReader();
    lector.onerror = reject;
    lector.onload = () => {
      const dataUrl = lector.result;
      // Extrae la parte base64 descartando el prefijo data:...;base64,
      const base64 = dataUrl.includes(',') ? dataUrl.split(',')[1] : dataUrl;
      resolve(base64);
    };
    lector.readAsDataURL(blob);
  });
}

/**
 * Comparte un archivo nativamente mediante @capacitor/filesystem y @capacitor/share.
 * En navegadores web, descarga automáticamente el archivo como fallback.
 *
 * @param {Object} params
 * @param {string} params.nombre_archivo - Nombre del archivo (ej: 'PED-0003.pdf').
 * @param {Blob} params.blob - Contenido binario del archivo.
 * @param {string} [params.titulo] - Título para la hoja de compartir nativa.
 * @param {string} [params.texto] - Texto explicativo a compartir.
 * @returns {Promise<{ ok: boolean, uri?: string }>}
 */
export async function compartir_archivo({ nombre_archivo, blob, titulo = 'Comprobante', texto = 'Comprobante de pedido' }) {
  if (!blob) {
    throw new Error('El archivo blob es requerido para compartir.');
  }

  const nombreSeguro = nombre_archivo || `archivo_${Date.now()}`;

  if (Capacitor.isNativePlatform()) {
    try {
      const base64 = await blob_a_base64(blob);

      // Escribir en la memoria caché del dispositivo
      const escrito = await Filesystem.writeFile({
        path: nombreSeguro,
        data: base64,
        directory: Directory.Cache,
      });

      // Abrir el diálogo de compartir nativo de Android
      await Share.share({
        title: titulo,
        text: texto,
        files: [escrito.uri],
        dialogTitle: titulo,
      });

      return { ok: true, uri: escrito.uri };
    } catch (error) {
      console.error('Error al compartir archivo en dispositivo nativo:', error);
      throw error;
    }
  } else {
    // Fallback para navegador web: descarga directa
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = nombreSeguro;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    return { ok: true };
  }
}

export const compartir_service = {
  compartir_archivo,
};

export default compartir_service;
