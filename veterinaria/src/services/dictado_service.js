import { SpeechRecognition } from '@capacitor-community/speech-recognition';
import { Capacitor } from '@capacitor/core';

let _instanciaWebSpeech = null;

/**
 * Servicio para entrada y dictado por voz (Unidad 10).
 * Soporta Capacitor SpeechRecognition en nativo y Web Speech API en navegadores.
 */
export const dictado_service = {
  /**
   * Comprueba si el dictado por voz está disponible en el dispositivo o navegador.
   * @returns {Promise<boolean>}
   */
  async dictado_disponible() {
    try {
      if (Capacitor.isNativePlatform() && Capacitor.isPluginAvailable('SpeechRecognition')) {
        const res = await SpeechRecognition.available();
        return !!res?.available;
      }

      // En entorno web / navegador
      if (typeof window !== 'undefined') {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
      }

      return false;
    } catch (error) {
      console.warn('Error al verificar disponibilidad de reconocimiento de voz:', error);
      return false;
    }
  },

  /**
   * Verifica y solicita permisos de reconocimiento de voz / micrófono si no fueron otorgados.
   * @returns {Promise<boolean>}
   */
  async asegurar_permiso() {
    try {
      if (Capacitor.isNativePlatform() && Capacitor.isPluginAvailable('SpeechRecognition')) {
        const estado = await SpeechRecognition.checkPermissions();
        if (estado.speechRecognition === 'granted') {
          return true;
        }

        const solicitud = await SpeechRecognition.requestPermissions();
        return solicitud.speechRecognition === 'granted';
      }

      // En navegador no hay método de permiso previo específico aparte de la consulta del navegador
      return true;
    } catch (error) {
      console.warn('Error al verificar/solicitar permisos de dictado:', error);
      return false;
    }
  },

  /**
   * Inicia la captura de audio y reconocimiento de voz en español argentino.
   * @param {string} idioma - Código de idioma (por defecto 'es-AR')
   * @returns {Promise<{ ok: boolean, texto: string, mensaje: string | null }>}
   */
  async escuchar(idioma = 'es-AR') {
    const disponible = await this.dictado_disponible();
    if (!disponible) {
      return {
        ok: false,
        texto: '',
        mensaje: 'El reconocimiento de voz no está disponible en este dispositivo.',
      };
    }

    const permiso = await this.asegurar_permiso();
    if (!permiso) {
      return {
        ok: false,
        texto: '',
        mensaje: 'Permiso de micrófono o reconocimiento de voz denegado.',
      };
    }

    // Modo nativo vía Capacitor Community Plugin
    if (Capacitor.isNativePlatform() && Capacitor.isPluginAvailable('SpeechRecognition')) {
      try {
        const res = await SpeechRecognition.start({
          language: idioma,
          maxResults: 1,
          partialResults: false,
          popup: true,
          prompt: 'Hablá ahora...',
        });

        const textoReconocido = res.matches && res.matches.length > 0 ? res.matches[0] : '';
        return {
          ok: true,
          texto: textoReconocido,
          mensaje: null,
        };
      } catch (error) {
        console.warn('Error durante el dictado en nativo:', error);
        return {
          ok: false,
          texto: '',
          mensaje: error?.message || 'No se reconoció ninguna frase o fue cancelado.',
        };
      }
    }

    // Modo Web Speech API en navegador
    if (typeof window !== 'undefined') {
      const SpeechRecognitionWeb = window.SpeechRecognition || window.webkitSpeechRecognition;
      if (SpeechRecognitionWeb) {
        return new Promise((resolve) => {
          try {
            const recognition = new SpeechRecognitionWeb();
            _instanciaWebSpeech = recognition;
            recognition.lang = idioma;
            recognition.interimResults = false;
            recognition.maxAlternatives = 1;

            let textoCapturado = '';

            recognition.onresult = (event) => {
              if (event.results && event.results.length > 0) {
                textoCapturado = event.results[0][0]?.transcript || '';
              }
            };

            recognition.onerror = (event) => {
              _instanciaWebSpeech = null;
              resolve({
                ok: false,
                texto: '',
                mensaje: event.error === 'no-speech' ? 'No se detectó voz.' : `Error: ${event.error}`,
              });
            };

            recognition.onend = () => {
              _instanciaWebSpeech = null;
              resolve({
                ok: !!textoCapturado,
                texto: textoCapturado,
                mensaje: textoCapturado ? null : 'No se detectó voz.',
              });
            };

            recognition.start();
          } catch (e) {
            _instanciaWebSpeech = null;
            resolve({
              ok: false,
              texto: '',
              mensaje: e.message || 'No se pudo iniciar el dictado en navegador.',
            });
          }
        });
      }
    }

    return {
      ok: false,
      texto: '',
      mensaje: 'Reconocimiento de voz no soportado.',
    };
  },

  /**
   * Detiene el reconocimiento en curso si existiera.
   */
  async detener() {
    try {
      if (Capacitor.isNativePlatform() && Capacitor.isPluginAvailable('SpeechRecognition')) {
        await SpeechRecognition.stop();
      } else if (_instanciaWebSpeech) {
        _instanciaWebSpeech.stop();
        _instanciaWebSpeech = null;
      }
    } catch (_e) {
      // Ignorar errores al detener
    }
  },
};

export default dictado_service;
