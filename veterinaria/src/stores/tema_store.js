import { reactive } from 'vue';
import { es_tema_oscuro, alternar_tema } from '@/config/tema';

const CLAVE_VIBRACION = 'veterinaria_vibracion';

function obtener_vibracion_inicial() {
  const guardado = localStorage.getItem(CLAVE_VIBRACION);
  // Por defecto true a menos que esté explícitamente guardado como 'false'
  return guardado !== 'false';
}

export const tema_store = reactive({
  oscuro: es_tema_oscuro(),
  vibracion: obtener_vibracion_inicial(),

  alternar_tema() {
    this.oscuro = alternar_tema();
    return this.oscuro;
  },

  establecer_vibracion(valor) {
    this.vibracion = Boolean(valor);
    localStorage.setItem(CLAVE_VIBRACION, this.vibracion ? 'true' : 'false');
    return this.vibracion;
  },

  alternar_vibracion() {
    return this.establecer_vibracion(!this.vibracion);
  },

  es_vibracion_activa() {
    return this.vibracion;
  },
});

export const preferencias_store = tema_store;
export default tema_store;
