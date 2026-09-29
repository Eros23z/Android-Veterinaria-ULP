import { BarcodeScanner, BarcodeFormat } from '@capacitor-mlkit/barcode-scanning';
import { Capacitor } from '@capacitor/core';

/**
 * Escanea un código QR utilizando el módulo nativo Google Barcode Scanner de ML Kit.
 * Maneja verificación de soporte, instalación automática del módulo de Google y permisos de cámara.
 * @returns {Promise<string|null>} Contenido del código QR escaneado o null si se canceló.
 */
export async function escanear_qr() {
  if (!Capacitor.isNativePlatform()) {
    // Fallback amigable para pruebas en navegador de escritorio
    const codigoIngresado = window.prompt('Entorno Web: Ingrese el código del comprobante a buscar (ej: PED-0001):');
    return codigoIngresado ? codigoIngresado.trim() : null;
  }

  try {
    // 1. Verificar soporte del dispositivo
    const { supported } = await BarcodeScanner.isSupported();
    if (!supported) {
      throw new Error('El escáner de códigos no está soportado en este dispositivo.');
    }

    // 2. Chequear y descargar módulo de Google Barcode Scanner si no está disponible
    const { available } = await BarcodeScanner.isGoogleBarcodeScannerModuleAvailable();
    if (!available) {
      await BarcodeScanner.installGoogleBarcodeScannerModule();
    }

    // 3. Verificar y solicitar permiso de cámara
    const permisos = await BarcodeScanner.checkPermissions();
    if (permisos.camera !== 'granted') {
      const solicitud = await BarcodeScanner.requestPermissions();
      if (solicitud.camera !== 'granted') {
        throw new Error('Permiso de cámara denegado. Conceda permiso para escanear el comprobante.');
      }
    }

    // 4. Ejecutar escaneo nativo filtrando únicamente formato QR
    const respuesta = await BarcodeScanner.scan({
      formats: [BarcodeFormat.QrCode],
    });

    if (respuesta.barcodes && respuesta.barcodes.length > 0) {
      return respuesta.barcodes[0].rawValue;
    }

    return null;
  } catch (error) {
    const mensaje = error?.message || '';
    if (mensaje.includes('cancelled') || mensaje.includes('Canceled') || mensaje.includes('User canceled')) {
      return null;
    }
    console.error('Error durante el escaneo de código QR:', error);
    throw error;
  }
}

export const qr_service = {
  escanear_qr,
};

export default qr_service;
