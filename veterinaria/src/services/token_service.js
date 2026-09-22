import { Capacitor } from '@capacitor/core';
import { SecureStorage } from '@aparajita/capacitor-secure-storage';

const CLAVE_ACCESS_TOKEN = 'vet_access_token';
const CLAVE_REFRESH_TOKEN = 'vet_refresh_token';

let _tokenMemoria = null;
let _refreshTokenMemoria = null;
let _inicializado = false;

const esNativo = Capacitor.isNativePlatform();

/**
 * Inicializa los tokens desde el almacenamiento persistente a la memoria.
 */
export async function inicializar_tokens() {
  if (_inicializado) return;

  try {
    if (esNativo) {
      _tokenMemoria = (await SecureStorage.get(CLAVE_ACCESS_TOKEN)) || null;
      _refreshTokenMemoria = (await SecureStorage.get(CLAVE_REFRESH_TOKEN)) || null;
    } else {
      _tokenMemoria = localStorage.getItem(CLAVE_ACCESS_TOKEN) || null;
      _refreshTokenMemoria = localStorage.getItem(CLAVE_REFRESH_TOKEN) || null;
    }
  } catch (error) {
    console.warn('Advertencia al leer tokens del almacenamiento seguro:', error);
    // Fallback a localStorage si falla SecureStorage
    _tokenMemoria = localStorage.getItem(CLAVE_ACCESS_TOKEN) || null;
    _refreshTokenMemoria = localStorage.getItem(CLAVE_REFRESH_TOKEN) || null;
  } finally {
    _inicializado = true;
  }
}

/**
 * Obtiene el access token de manera sincrónica desde memoria.
 * @returns {string|null}
 */
export function obtener_token() {
  return _tokenMemoria;
}

/**
 * Obtiene el refresh token de manera sincrónica desde memoria.
 * @returns {string|null}
 */
export function obtener_refresh_token() {
  return _refreshTokenMemoria;
}

/**
 * Guarda los tokens en memoria y en almacenamiento seguro / fallback web.
 * @param {string} accessToken
 * @param {string} refreshToken
 */
export async function guardar_tokens(accessToken, refreshToken) {
  _tokenMemoria = accessToken;
  _refreshTokenMemoria = refreshToken;
  _inicializado = true;

  try {
    if (esNativo) {
      if (accessToken) await SecureStorage.set(CLAVE_ACCESS_TOKEN, accessToken);
      if (refreshToken) await SecureStorage.set(CLAVE_REFRESH_TOKEN, refreshToken);
    } else {
      if (accessToken) localStorage.setItem(CLAVE_ACCESS_TOKEN, accessToken);
      if (refreshToken) localStorage.setItem(CLAVE_REFRESH_TOKEN, refreshToken);
    }
  } catch (error) {
    console.warn('Error al persistir en almacenamiento seguro, usando fallback web:', error);
    if (accessToken) localStorage.setItem(CLAVE_ACCESS_TOKEN, accessToken);
    if (refreshToken) localStorage.setItem(CLAVE_REFRESH_TOKEN, refreshToken);
  }
}

/**
 * Limpia los tokens de la memoria y del almacenamiento persistente.
 */
export async function limpiar_tokens() {
  _tokenMemoria = null;
  _refreshTokenMemoria = null;

  try {
    if (esNativo) {
      await SecureStorage.remove(CLAVE_ACCESS_TOKEN).catch(() => {});
      await SecureStorage.remove(CLAVE_REFRESH_TOKEN).catch(() => {});
    }
    localStorage.removeItem(CLAVE_ACCESS_TOKEN);
    localStorage.removeItem(CLAVE_REFRESH_TOKEN);
  } catch (error) {
    console.warn('Error al limpiar almacenamiento seguro:', error);
    localStorage.removeItem(CLAVE_ACCESS_TOKEN);
    localStorage.removeItem(CLAVE_REFRESH_TOKEN);
  }
}

export const token_service = {
  inicializar_tokens,
  obtener_token,
  obtener_refresh_token,
  guardar_tokens,
  limpiar_tokens,
};

export default token_service;
