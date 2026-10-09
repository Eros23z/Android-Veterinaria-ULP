import { Geolocation } from '@capacitor/geolocation';
import { Capacitor } from '@capacitor/core';

/**
 * Obtiene la posición geográfica actual del dispositivo.
 * Solicita permisos si es necesario y captura coordenadas con alta precisión.
 * @returns {Promise<{ ok: boolean, ubicacion: { latitud: number, longitud: number, precision: number } | null, mensaje: string | null }>}
 */
export async function obtener_ubicacion_actual() {
  try {
    // Verificar si el plugin está disponible en la plataforma actual
    if (!Capacitor.isPluginAvailable('Geolocation') && !navigator.geolocation) {
      return {
        ok: false,
        ubicacion: null,
        mensaje: 'El servicio de geolocalización no está disponible en este dispositivo.',
      };
    }

    // En plataformas móviles con Capacitor, verificar / solicitar permisos
    if (Capacitor.isNativePlatform()) {
      try {
        const estadoPermisos = await Geolocation.checkPermissions();
        if (estadoPermisos.location !== 'granted' && estadoPermisos.coarseLocation !== 'granted') {
          const solicitud = await Geolocation.requestPermissions({ permissions: ['location', 'coarseLocation'] });
          if (solicitud.location !== 'granted' && solicitud.coarseLocation !== 'granted') {
            return {
              ok: false,
              ubicacion: null,
              mensaje: 'Permiso de ubicación denegado por el usuario.',
            };
          }
        }
      } catch (permError) {
        console.warn('Error al verificar permisos de geolocalización:', permError);
      }
    }

    // Obtener posición actual con alta precisión y tiempo límite de 12 segundos
    const posicion = await Geolocation.getCurrentPosition({
      enableHighAccuracy: true,
      timeout: 12000,
      maximumAge: 5000,
    });

    if (!posicion || !posicion.coords) {
      return {
        ok: false,
        ubicacion: null,
        mensaje: 'No se recibieron coordenadas válidas del sensor.',
      };
    }

    return {
      ok: true,
      ubicacion: {
        latitud: Number(posicion.coords.latitude.toFixed(6)),
        longitud: Number(posicion.coords.longitude.toFixed(6)),
        precision: Math.round(posicion.coords.accuracy || 0),
      },
      mensaje: null,
    };
  } catch (error) {
    console.error('Error al obtener ubicación actual:', error);
    let mensajeAmigable = 'No se pudo obtener la ubicación actual.';
    if (error?.message) {
      if (error.message.includes('denied') || error.message.includes('permission')) {
        mensajeAmigable = 'Permiso de ubicación denegado o no otorgado.';
      } else if (error.message.includes('timeout') || error.message.includes('timed out')) {
        mensajeAmigable = 'Se agotó el tiempo de espera al capturar el GPS.';
      } else {
        mensajeAmigable = error.message;
      }
    }
    return {
      ok: false,
      ubicacion: null,
      mensaje: mensajeAmigable,
    };
  }
}

/**
 * Formatea coordenadas a string decimal limpio para evitar dobles puntos.
 * @param {number|string|null} latitud
 * @param {number|string|null} longitud
 * @returns {string}
 */
export function formatear_coordenadas(latitud, longitud) {
  if (latitud == null || longitud == null) return '';
  let lat = parseFloat(latitud);
  let lng = parseFloat(longitud);
  if (isNaN(lat) || isNaN(lng)) return '';
  while (Math.abs(lat) > 90 && lat !== 0) lat /= 10;
  while (Math.abs(lng) > 180 && lng !== 0) lng /= 10;
  return `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
}

/**
 * Genera una URL externa de Google Maps utilizando coordenadas o dirección textual.
 * Sanea con parseFloat y arma la query limpia sin espacios.
 * @param {number|string|null} latitud
 * @param {number|string|null} longitud
 * @param {string|null} direccion
 * @returns {string} URL formateada para abrir en navegador o app de mapas.
 */
export function url_mapa(latitud, longitud, direccion = '') {
  let lat = parseFloat(latitud);
  let lng = parseFloat(longitud);
  if (!isNaN(lat) && !isNaN(lng)) {
    while (Math.abs(lat) > 90 && lat !== 0) lat /= 10;
    while (Math.abs(lng) > 180 && lng !== 0) lng /= 10;
    return `https://www.google.com/maps/search/?api=1&query=${lat.toFixed(6)},${lng.toFixed(6)}`;
  }
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(direccion || '')}`;
}

export const geolocalizacion_service = {
  obtener_ubicacion_actual,
  formatear_coordenadas,
  url_mapa,
};

export default geolocalizacion_service;
